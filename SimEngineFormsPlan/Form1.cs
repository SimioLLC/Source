﻿using SimEngineLibrary;
using LoggertonHelpers;
using SimioAPI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SimEngineFormsPlan
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

            timerLogs.Enabled = true;

            string extensionsPath = System.AppDomain.CurrentDomain.BaseDirectory;
            textExtensionsPath.Text = extensionsPath;

        }

        private bool LoadProject(string extensionsPath, string projectPath, out string explanation)
        {
            explanation = "";

            Cursor.Current = Cursors.WaitCursor;
            try
            {
                List<string> warningList = new List<string>();
                ISimioProject project = SimEngineHelpers.LoadProject(extensionsPath, projectPath, warningList, out explanation);
                if (project == null)
                {
                    explanation = $"Cannot load project={projectPath}. Reason={explanation}";
                    return false;
                }

                comboHeadlessRunModels.Items.Clear();
                comboHeadlessRunModels.DisplayMember = "Name";
                foreach (var model in project.Models)
                {
                    comboHeadlessRunModels.Items.Add(model);
                }

                IModel defaultModel = comboHeadlessRunModels.Items[1] as IModel;
                comboHeadlessRunModels.Enabled = true;

                return true;
            }
            catch (Exception ex)
            {
                explanation = $"Project={projectPath} Err={ex.Message}";
                return false;
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }

        }

        private void Alert(string msg)
        {
            Loggerton.Instance.LogIt(EnumLogFlags.Error, msg);
            MessageBox.Show(msg);
        }

        private void Alert(EnumLogFlags flags, string msg)
        {
            Loggerton.Instance.LogIt(flags, msg);
            MessageBox.Show(msg);
        }

        private void Logit(string msg)
        {
            if (msg.ToLower().StartsWith("info:"))
                Loggerton.Instance.LogIt(EnumLogFlags.Information, msg);
            else
                Loggerton.Instance.LogIt(EnumLogFlags.Error, msg);
        }

        private void buttonHeadlessSelectModel_Click(object sender, EventArgs e)
        {
            textHeadlessProjectFile.Text = GetProjectFile();
        }

        private void timerLogs_Tick(object sender, EventArgs e)
        {
            textLogs.Text = Loggerton.Instance.ShowLogs();
        }

        private void buttonRunPlan_Click_1(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                string modelName = comboHeadlessRunModels.Text;

                string projectPath = textHeadlessProjectFile.Text;
                string extensionsPath = textExtensionsPath.Text;
                Logit($"Info: Running Model={modelName} Plan. ExtensionsPath={extensionsPath}");
                List<string> warningList = new List<string>();

                if (!SimEngineHelpers.RunModelPlan(extensionsPath, projectPath, modelName, 
                    cbHeadlessRunRiskAnalysis.Checked, 
                    cbHeadlessSaveModelAfterRun.Checked,
                    cbHeadlessRunPublishPlanAfterRun.Checked, 
                    warningList, out string explanation))
                {
                    Alert(explanation);
                }
                else
                {
                    Alert(EnumLogFlags.Information, $"Success! \nProject={projectPath} Model={modelName} performed the actions successfully. Check the logs for more information.");
                }
            }
            catch (Exception ex)
            {
                Alert($"RunPlan: Err={ex.Message}");
                throw;
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }

        }

        private void buttonLoadSimioProject_Click(object sender, EventArgs e)
        {
            string projectPath = textHeadlessProjectFile.Text;
            string extensionsPath = textExtensionsPath.Text;

            if (!LoadProject(extensionsPath, projectPath, out string explanation))
                Alert($"{explanation}");

        }

        private void buttonSelectExtensionsPath_Click(object sender, EventArgs e)
        {
            string extensionsPath = GetExtensionsFolder();
            textExtensionsPath.Text = extensionsPath;
        }

        private void actionsToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void fileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
            Application.Exit();
        }

        /// <summary>
        /// Prompt the user for a Simio project file.
        /// </summary>
        /// <returns></returns>
        public static string GetProjectFile()
        {
            try
            {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Multiselect = false;
                dialog.Filter = "Simio Project|*.spfx";

                DialogResult result = dialog.ShowDialog();

                if (result != DialogResult.OK)
                    return string.Empty;

                return dialog.FileName;
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Cannot get project file. Err={ex.Message}");
            }
        }

        /// <summary>
        /// Prompt the user for a Simio project file.
        /// </summary>
        /// <returns></returns>
        public static string GetExtensionsFolder()
        {
            try
            {
                FolderBrowserDialog dialog = new FolderBrowserDialog();
                dialog.SelectedPath = "";
                dialog.ShowNewFolderButton = false;

                DialogResult result = dialog.ShowDialog();

                if (result != DialogResult.OK)
                    return string.Empty;

                return dialog.SelectedPath;
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Cannot get Extensions folder. Err={ex.Message}");
            }
        }

    }
}
