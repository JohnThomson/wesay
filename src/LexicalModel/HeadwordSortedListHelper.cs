using System;
using System.Collections.Generic;
using Db4objects.Db4o;
using Palaso.Base32;
using WeSay.Data;
using WeSay.Foundation;
using WeSay.Language;

namespace WeSay.LexicalModel
{
    public class HeadwordSortedListHelper: ISortHelper<string, LexEntry>
    {
        Db4oDataSource _db4oData; // for data
        WritingSystem _writingSystem;
        
        public HeadwordSortedListHelper(Db4oDataSource db4oData, 
                                  WritingSystem writingSystem)
        {
            if(db4oData == null)
            {
                throw new ArgumentNullException("db4oData");
            }
            if (writingSystem == null)
            {
                throw new ArgumentNullException("writingSystem");
            }
            
            _db4oData = db4oData;
            _writingSystem = writingSystem;
        }

        public HeadwordSortedListHelper(WritingSystem writingSystem)
        {
            if (writingSystem == null)
            {
                throw new ArgumentNullException("writingSystemId");
            }

            _writingSystem = writingSystem;
        }

        #region IDb4oSortHelper<string,LexEntry> Members

        public IComparer<string> KeyComparer
        {
            get
            {
                return StringComparer.Ordinal;
            }
        }


        public List<KeyValuePair<string, long>> GetKeyIdPairs()
        {
            if (_db4oData != null)
            {
                List<KeyValuePair<string, long>> pairs = new List<KeyValuePair<string, long>>();

                IObjectSet set = _db4oData.Data.Get(typeof (LexEntry));
                foreach (LexEntry entry in set)
                {
                    foreach (string key in GetKeys(entry))
                    {
                        pairs.Add(new KeyValuePair<string, long>(key, _db4oData.Data.Ext().GetID(entry)));
                    }
                }
                return pairs;
            }
            throw new InvalidOperationException();
        }

        public IEnumerable<string> GetKeys(LexEntry entry)
        {
            List<string> keys = new List<string>();
            byte[] keydata = _writingSystem.GetSortKey(entry.GetHeadWord(_writingSystem.Id)).KeyData;
            string key = Base32Convert.ToBase32String(keydata, Base32FormattingOptions.None);
            int homographNumber = ComputeHomographNumber(entry);
            key += "_" + homographNumber.ToString("000000"); 
            
            keys.Add(key);
            return keys;
        }

        private int ComputeHomographNumber(LexEntry entry)
        {
            return 0;
            //todo: look at @order and the planned-for order-in-lift field on LexEntry
        }


        public string Name
        {
            get
            {
                return "Headwords sorted by " + _writingSystem.Id;
            }
        }

        public override int GetHashCode()
        {
            return _writingSystem.GetHashCode();
        }

        #endregion
    }
}