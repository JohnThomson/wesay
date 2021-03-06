using System;
using System.IO;
using System.Windows.Forms;
using NUnit.Extensions.Forms;
using NUnit.Framework;
using SIL.TestUtilities;

namespace WeSay.ConfigTool.Tests
{
	[TestFixture]
	public class BackupPlanControlTests
	{
		private ConfigurationWindow _window;

		[SetUp]
		public void Setup()
		{
			SIL.Reporting.ErrorReport.IsOkToInteractWithUser = false;
		}

		private void GoToBackupTab()
		{
			ToolStrip toolstrip = (ToolStrip)_window.Controls.Find("_areasToolStrip", true)[0];
			foreach (ToolStripButton button in toolstrip.Items)
			{
				if (button.Text.Contains("Backup"))
				{
					button.PerformClick();
					Application.DoEvents();
					break;
				}
			}
		}

		[TearDown]
		public void TearDown()
		{
		}



		[Test, RequiresSTA]
		public void SetValues_Reopen_HasSameValues()
		{
					using (TemporaryFolder backupHere = new TemporaryFolder("backupLocationForWeSayBackupPlanTests"))
					{
						using (TemporaryFolder tempFolder = new TemporaryFolder("backupPlanControlTests"))
						{
							try
							{
								CreateNewAndGotoBackupControl(tempFolder.Path);

								TextBoxTester t = new TextBoxTester("_pathText", _window);
								t.Properties.Text = backupHere.Path;
								CloseApp();

								//now reopen
								OpenExisting(tempFolder.Path);
								GoToBackupTab();
								t = new TextBoxTester("_pathText", _window);

								Assert.AreEqual(backupHere.Path, t.Properties.Text);
							}
							finally
							{
								CloseApp();
							}
						}
					}
		}


		private void CloseApp()
		{
			if(_window !=null)
			{
				_window.Close();
				_window.Dispose();
				_window = null;
			}
		}


		private void CreateNewAndGotoBackupControl(string directoryPath)
		{
			_window = new ConfigurationWindow(new string[] { });
			_window.Show();
			_window.CreateAndOpenProject(directoryPath, "th", "Thai");
			GoToBackupTab();
		   // return new ComboBoxTester("_languageCombo", _window);
		}
//
//        private ComboBoxTester GoToTabAndGetLanguageCombo()
//        {
//            GoToBackupTab();
//            ComboBoxTester t = new ComboBoxTester("_languageCombo", _window);
//            return t;
//        }

		private void OpenExisting(string path)
		{
			_window = new ConfigurationWindow(new string[] { });
			_window.Show();
			_window = new ConfigurationWindow(new string[] { });
			_window.Show();
			_window.OpenProject(path);
		}

	}
}
