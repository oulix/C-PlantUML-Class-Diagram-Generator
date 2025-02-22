﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace PlantUMLCodeGeneratorGUI
{
    public partial class frmMain : Form
    {
        private string _lastVisitedDirectory = null;

        public frmMain()
        {
            InitializeComponent();
        }

        private void btnAddFolder_Click(object sender, EventArgs e)
        {
            //using (var fbd = new FolderBrowserDialog())
            //{
            //    fbd.SelectedPath = _lastVisitedDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            //    fbd.SelectedPath = @"D:\documents\marketdata\include";
            //    fbd.Description = "Please select the folder which contains the header files";

            //    if (fbd.ShowDialog() == DialogResult.OK && (lstFolderList.Items.Cast<string>().Any(i => i == fbd.SelectedPath) == false))
            //    {
            //        lstFolderList.Items.Add(fbd.SelectedPath);
            //        _lastVisitedDirectory = fbd.SelectedPath;

            //        btnGenerate.Enabled = true;
            //    }
            //}

            /*
            //using (var fbd = new OpenFileDialog())
            //{
            //    fbd.ValidateNames = false;
            //    fbd.CheckFileExists = false;
            //    fbd.CheckPathExists = true;                
            //    fbd.InitialDirectory = _lastVisitedDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop);                
            //    fbd.Title= "Please select the folder which contains the header files";

            //    if (fbd.ShowDialog() == DialogResult.OK)
            //    {
            //        string folderPath = Path.GetDirectoryName(fbd.FileName);
            //        lstFolderList.Items.Add(folderPath);
            //        _lastVisitedDirectory = folderPath;

            //        btnGenerate.Enabled = true;
            //    }
            //}
            */

            FolderPicker dlg = new FolderPicker();
            {
                dlg.InputPath = @"c:\windows\system32";
                IntPtr nullptr = IntPtr.Zero;
                if (dlg.ShowDialog(nullptr, true) == true)
                {
                    string folderPath = dlg.ResultPath;
                    lstFolderList.Items.Add(folderPath);
                    _lastVisitedDirectory = folderPath;

                    btnGenerate.Enabled = true;
                }
            }
        }

        private void btnRemoveFolder_Click(object sender, EventArgs e)
        {
            if (lstFolderList.Items.Count > 0)
            {
                var selectedItems = lstFolderList.SelectedItems.OfType<object>().ToArray();
                for (var i = 0; i < selectedItems.Length; i++)
                {
                    lstFolderList.Items.Remove(selectedItems[i]);
                }
            }

            btnGenerate.Enabled = lstFolderList.Items.Count > 0;
        }

        private void lstFolderList_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnRemoveFolder.Enabled = lstFolderList.SelectedItems.Count > 0;
        }

        private void btnGenerate_Click(object sender, EventArgs e)
        {
            var objResult = frmLoadingDialog.ShowWindow(o =>
            {
                var headerFiles = lstFolderList.Items.Cast<string>().Select(i => Directory.GetFiles(i, "*.h", SearchOption.AllDirectories)).SelectMany(i => i);

                var completeContent = "";
                foreach (var file in headerFiles)
                {
                    frmLoadingDialog.UpdateProgressText("Processing File : " + file);
                    completeContent += Environment.NewLine + File.ReadAllText(file);
                }

                try
                {
                    Namespace defaultNamespace;
                    var output = Processor.Process(completeContent, new Settings(chkShowOverriddenMembers.Checked, chkShowPrivateMembers.Checked, chkShowProtectedMembers.Checked), out defaultNamespace);
                    
                    var content = "";
                    content += "@startuml" + Environment.NewLine;
                    content += "left to right direction" + Environment.NewLine;
                    content += Environment.NewLine;
                    content += output + Environment.NewLine;
                    content += Environment.NewLine;
                    content += "@enduml";

                    return content;
                }
                catch (Exception ex)
                {
                    return ex;
                }
            }, null, "Processing Files...", false);

            var knownBug = objResult as KnownBugCapturedException;
            if (knownBug != null)
            {
                var nl = Environment.NewLine;
                var decision = MessageBox.Show($@"Looks like you are facing one of our know issues. The following is the explanation given, {nl}{nl}'{knownBug.Message}'{nl}{nl}Would you to visit the issue page ?", @"Operation Failed Due to Know Issue", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
                if (decision == DialogResult.Yes)
                {
                    Process.Start($"https://github.com/sathukorale/C-PlantUML-Class-Diagram-Generator/issues/{knownBug.BugId}");
                }

                return;
            }

            var unknownBug = objResult as Exception;
            if (unknownBug != null)
            {
                var decision = MessageBox.Show($@"Looks like you are facing a bug we have never discovered. Would you like to create a new issue on our Github page ?", @"Operation Failed Due to Unknown Issue", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
                if (decision == DialogResult.Yes)
                {
                    var title = Uri.EscapeDataString($"[Bug][GenerationLogic] {unknownBug.Message.Trim()}");
                    var message = Uri.EscapeDataString($"We are facing this issue `{unknownBug.Message.Trim()}` when trying to use the app. The following is the corresponding stack trace.\n\n```\n{unknownBug.StackTrace}\n```\n");

                    Process.Start($@"https://github.com/sathukorale/C-PlantUML-Class-Diagram-Generator/issues/new?title={title}&body={message}&assignees=sathukorale&labels=bug,to-be-classified");
                }

                return;
            }

            var generatedContent = objResult as string;

            using (var sfd = new SaveFileDialog())
            {
                sfd.OverwritePrompt = true;
                sfd.Filter = "WSD File|*.wsd";
                sfd.Title = "Save Generated PlantUML File";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    if (File.Exists(sfd.FileName))
                        File.Delete(sfd.FileName);

                    File.WriteAllText(sfd.FileName, generatedContent);
                    MessageBox.Show("The PlantUML script was generated and saved successfully !", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }
}
