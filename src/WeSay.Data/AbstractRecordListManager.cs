	using System;
using System.Collections;

namespace WeSay.Data
{
	public class DeletedItemEventArgs:EventArgs
	{
		private readonly object _itemDeleted;
		public DeletedItemEventArgs(object itemDeleted)
		{
			_itemDeleted = itemDeleted;
		}
		public object ItemDeleted
		{
			get { return this._itemDeleted; }
		}
	}
	public abstract class AbstractRecordListManager : IRecordListManager
	{
		private Hashtable _filteredRecordLists;
		protected AbstractRecordListManager()
		{
			_filteredRecordLists = new Hashtable();
		}

		abstract protected IRecordList<T> CreateMasterRecordList<T>() where T : class, new();
		abstract protected IRecordList<T> CreateFilteredRecordList<T>(IFilter<T> filter) where T : class, new();

		#region IRecordListManager Members

		protected virtual IRecordList<T> CreateFilteredRecordListUnlessSlow<T>(IFilter<T> filter) where T: class, new()
		{
			return null;
		}

		private static string RecordListKey<T>(string filterName) where T : class, new()
		{
			return typeof(T).FullName + filterName;
		}

		public void Register<T>(IFilter<T> filter) where T : class, new()
		{
			if (!FilteredRecordLists.ContainsKey(RecordListKey<T>(filter.Key)))
			{
				FilteredRecordLists.Add(RecordListKey<T>(filter.Key), CreateFilteredRecordListUnlessSlow<T>(filter));
			}
		}

		public IRecordList<T> GetListOfType<T>() where T : class, new()
		{
			if (!FilteredRecordLists.ContainsKey(RecordListKey<T>(String.Empty)))
			{
				IRecordList<T> MasterRecordList = CreateMasterRecordList<T>();
				MasterRecordList.DeletingRecord += new EventHandler<RecordListEventArgs<T>>(MasterRecordList_DeletingRecord<T>);
				FilteredRecordLists.Add(RecordListKey<T>(String.Empty), MasterRecordList);
			}
			return (IRecordList<T>)FilteredRecordLists[RecordListKey<T>(String.Empty)];
		}

		void MasterRecordList_DeletingRecord<T>(object sender, RecordListEventArgs<T> e) where T : class, new()
		{
			DataDeleted.Invoke(this, new DeletedItemEventArgs(e.Item));
		}

		public IRecordList<T> GetListOfTypeFilteredFurther<T>(IFilter<T> filter) where T : class, new()
		{
			if(filter == null)
			{
				throw new ArgumentNullException();
			}
			if (!FilteredRecordLists.ContainsKey(RecordListKey<T>(filter.Key)))
			{
				throw new InvalidOperationException("Filter must be registered before it can be retrieved with GetListOfType.");
			}
			IRecordList<T> recordList = (IRecordList<T>)FilteredRecordLists[RecordListKey<T>(filter.Key)];
			if (recordList == null)
			{
				recordList = CreateFilteredRecordList<T>(filter);
				FilteredRecordLists[RecordListKey<T>(filter.Key)] = recordList;
			}
			return recordList;
		}

		#endregion

		#region IDisposable Members
		#if DEBUG
		~AbstractRecordListManager()
		{
			if (!this._disposed)
			{
				throw new InvalidOperationException("Disposed not explicitly called on " + GetType().FullName + ".");
			}
		}
		#endif

		private bool _disposed;

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public bool IsDisposed
		{
			get
			{
				return _disposed;
			}
			private
			set
			{
				_disposed = value;
			}
		}

		protected Hashtable FilteredRecordLists
		{
			get { return this._filteredRecordLists; }
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				if (disposing)
				{
					// dispose-only, i.e. non-finalizable logic
					foreach (DictionaryEntry dictionaryEntry in FilteredRecordLists)
					{
						IDisposable disposable = dictionaryEntry.Value as IDisposable;
						if (disposable != null)
						{
							disposable.Dispose();
						}
					}
					_filteredRecordLists = null;
				}

				// shared (dispose and finalizable) cleanup logic
				IsDisposed = true;
			}
		}

		protected void VerifyNotDisposed()
		{
			if (IsDisposed)
			{
				throw new ObjectDisposedException(GetType().FullName);
			}
		}
		#endregion

		/// <summary>
		/// Called whenever the record list knows some data was committed to the database
		/// </summary>
		public event EventHandler DataCommitted = delegate {};
		public event EventHandler<DeletedItemEventArgs> DataDeleted = delegate
		{
		};

//        protected void OnDataCommitted(object sender, EventArgs e)
//        {
//            if (this.DataCommitted != null)
//            {
//                this.DataCommitted.Invoke(this, null);
//            }
//        }

		/// <summary>
		/// Call this, for example, when switching records in the gui. You don't need to know
		/// whether a commit is pending or not.
		/// </summary>
		public void GoodTimeToCommit()
		{
			if (CommitIfNeeded())
			{
				DataCommitted.Invoke(this, null);
			}
		}

		abstract protected bool CommitIfNeeded();
	}
}
