using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using WeSay.Language;
using WeSay.LexicalModel;
using WeSay.Project;
using WeSay.Data;

namespace WeSay.CommonTools
{
    public partial class DashboardControl : UserControl, ITask, IFinishCacheSetup
    {
        IRecordListManager _recordListManager;
        ICurrentWorkTask _currentWorkTaskProvider;
        IList<TaskIndicator> _taskIndicators;
        private bool _isActive;

        public DashboardControl(IRecordListManager recordListManager, ICurrentWorkTask currentWorkTaskProvider)
        {
            if (recordListManager == null)
            {
                throw new ArgumentNullException("recordListManager");
            }
            if (currentWorkTaskProvider == null)
            {
                throw new ArgumentNullException("currentWorkTaskProvider");
            }
            _taskIndicators = new List<TaskIndicator>();
            _recordListManager = recordListManager;
            _currentWorkTaskProvider = currentWorkTaskProvider;
            InitializeComponent();
            ContextMenu = new ContextMenu();
            ContextMenu.MenuItems.Add("Configure this project...", new EventHandler(OnRunConfigureTool));
       }

        
        private static void OnRunConfigureTool(object sender, EventArgs e)
        {
            string dir = Directory.GetParent(Application.ExecutablePath).FullName;
            ProcessStartInfo startInfo =
                new ProcessStartInfo(Path.Combine(dir, "WeSay Configuration Tool.exe"),
                                     string.Format("\"{0}\"", WeSayWordsProject.Project.ProjectDirectoryPath));
            try
            {
                Process.Start(startInfo);
            }
            catch
            {
                Reporting.ErrorReporter.ReportNonFatalMessage("Could not start "+startInfo.FileName);
            }

            Application.Exit();
        }

        private TaskIndicator TaskIndicatorFromTask(ITask task)
        {
            TaskIndicator taskIndicator = new TaskIndicator(task);
            taskIndicator.selected += new EventHandler(OnTaskIndicatorSelected);
            _taskIndicators.Add(taskIndicator);
            return taskIndicator;
        }

        void OnTaskIndicatorSelected(object sender, EventArgs e)
        {
            TaskIndicator taskIndicator = (TaskIndicator) sender;
            _currentWorkTaskProvider.ActiveTask = taskIndicator.Task;
        }
                
        private void AddIndicator(TaskIndicator indicator)
        {
            Panel indentPanel = new Panel();
            indicator.Left = 70;
            indicator.Top = 0;
            indentPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            indentPanel.Size = new Size(indicator.Right, indicator.Height);
            indentPanel.Controls.Add(indicator);
            this._vbox.AddControlToBottom(indentPanel);
        }

        #region ITask
        public void Activate()
        {
            _vbox.SuspendLayout();
            Size originalSize = _vbox.Size; // this prevents us from growing wider than we should when we need a vertical scroll bar (and thus preventing the horizontal scroll bar)
            _vbox.Size = new Size(10, 10);
            if (IsActive)
            {
                throw new InvalidOperationException("Activate should not be called when object is active.");
            }
            this._projectNameLabel.Text = BasilProject.Project.Name;
            DictionaryStatusControl status = new DictionaryStatusControl(_recordListManager.GetListOfType<LexEntry>());
            this._vbox.AddControlToBottom(status);
            
            ITask currentWorkTask = _currentWorkTaskProvider.CurrentWorkTask;
            if (currentWorkTask != null)
            {
                _vbox.AddControlToBottom(new CurrentTaskIndicatorControl(TaskIndicatorFromTask(currentWorkTask)));
            }

            IList<ITask> taskList = ((WeSayWordsProject)BasilProject.Project).Tasks;
            foreach (ITask task in taskList)
            {
                if (task != this && task.IsPinned )
                {
                    AddIndicator(TaskIndicatorFromTask(task));
                }
            }
            GroupHeader header = new GroupHeader();
            header.Name = "Tasks";
            AddGroupHeader(header);

            foreach (ITask task in taskList)
            {
                if (task != this && !task.IsPinned)//&& (task != currentWorkTask))
                {
                    AddIndicator(TaskIndicatorFromTask(task));
                }
            } 
            
            _isActive = true;
            _vbox.VerticalScroll.Visible = true;

            _vbox.ResumeLayout(true);
            _vbox.Size = originalSize;
        }

        private void AddGroupHeader(GroupHeader header)
        {
            header.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
//            indentPanel.Size = new Size(indicator.Right, indicator.Height);
//            indentPanel.Controls.Add(indicator);
            this._vbox.AddControlToBottom(header);
        }

        public void Deactivate()
        {
            if(!IsActive)
            {
                throw new InvalidOperationException("Deactivate should only be called once after Activate.");
            }
            foreach (TaskIndicator taskIndicator in _taskIndicators)
            {
                taskIndicator.selected -= OnTaskIndicatorSelected;
            }
            this._vbox.Clear();
            _isActive = false;
        }

        public bool IsActive
        {
            get { return this._isActive; }
        }

        public string Label
        {
            get { return StringCatalog.Get("Home"); }
        }

        public Control Control
        {
            get { return this; }
        }

        public bool IsPinned
        {
            get
            {
                return true;
            }
        }

        public string Status
        {
            get
            {
                return string.Empty;
            }
        }
        public string ExactStatus
        {
            get
            {
                return Status;
            }
        }

        public string Description
        {
            get
            {
                return StringCatalog.Get("Switch tasks and see current status of tasks");
            }
        }

        #endregion

        #region IFinishCacheSetup Members

        public void FinishCacheSetup()
        {
            Activate();
            Deactivate();
        }

        #endregion
    }
}
