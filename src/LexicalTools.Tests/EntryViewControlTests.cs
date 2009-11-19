using System.IO;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using Palaso.LexicalModel.Options;
using Palaso.Lift.Options;
using Palaso.TestUtilities;
using WeSay.LexicalModel;
using WeSay.Project;
using WeSay.UI;
using WeSay.UI.TextBoxes;

namespace WeSay.LexicalTools.Tests
{
	[TestFixture]
	public class EntryViewControlTests
	{
		private TemporaryFolder _tempFolder;
		private LexEntryRepository _lexEntryRepository;
		private string _filePath;

		private LexEntry empty;
		private LexEntry apple;
		private LexEntry banana;
		private LexEntry car;
		private LexEntry bike;
		private ViewTemplate _viewTemplate;
		private string _primaryMeaningFieldName;

		[SetUp]
		public void SetUp()
		{
			WeSayWordsProject.InitializeForTests();

			_tempFolder = new TemporaryFolder();
			_filePath = _tempFolder.GetTemporaryFile();
			_lexEntryRepository = new LexEntryRepository(_filePath);

#if GlossMeaning
		   _primaryMeaningFieldName = Field.FieldNames.SenseGloss.ToString();
#else
			_primaryMeaningFieldName = LexSense.WellKnownProperties.Definition;
#endif

			string[] analysisWritingSystemIds = new string[]
													{
															BasilProject.Project.WritingSystems.
																	TestWritingSystemAnalId
													};
			string[] vernacularWritingSystemIds = new string[]
													  {
															  BasilProject.Project.WritingSystems.
																	  TestWritingSystemVernId
													  };
			RtfRenderer.HeadWordWritingSystemId = vernacularWritingSystemIds[0];

			_viewTemplate = new ViewTemplate();
			_viewTemplate.Add(new Field(Field.FieldNames.EntryLexicalForm.ToString(),
										"LexEntry",
										vernacularWritingSystemIds));
			_viewTemplate.Add(new Field(_primaryMeaningFieldName,
										"LexSense",
										analysisWritingSystemIds));
			_viewTemplate.Add(new Field(Field.FieldNames.ExampleSentence.ToString(),
										"LexExampleSentence",
										vernacularWritingSystemIds));
			_viewTemplate.Add(new Field(Field.FieldNames.ExampleTranslation.ToString(),
										"LexExampleSentence",
										analysisWritingSystemIds));

			empty = CreateTestEntry("", "", "");
			apple = CreateTestEntry("apple", "red thing", "An apple a day keeps the doctor away.");
			banana = CreateTestEntry("banana", "yellow food", "Monkeys like to eat bananas.");
			car = CreateTestEntry("car",
								  "small motorized vehicle",
								  "Watch out for cars when you cross the street.");
			bike = CreateTestEntry("bike", "vehicle with two wheels", "He rides his bike to school.");
		}

		[TearDown]
		public void TearDown()
		{
			_lexEntryRepository.Dispose();
			_tempFolder.Delete();
		}

		[Test]
		public void CreateWithInventory()
		{
			using (EntryViewControl entryViewControl = new EntryViewControl())
			{
				Assert.IsNotNull(entryViewControl);
			}
		}

		[Test]
		public void NullDataSource_ShowsEmpty()
		{
			using (EntryViewControl entryViewControl = CreateForm(null, false))
			{
				Assert.AreEqual(string.Empty, entryViewControl.ControlFormattedView.Text);
			}
		}

		[Test]
		public void FormattedView_ShowsCurrentEntry()
		{
			TestEntryShows(apple);
			TestEntryShows(banana);
		}

		[Test]
		public void FormattedView_ShowsPartOfSpeech()
		{
			LexSense sense = apple.Senses[0];
			OptionRef o;
			o = sense.GetOrCreateProperty<OptionRef>("POS");
			o.Value = "noun";
			//nb: this is the key, which for noun happens to be the English display name tested for below
			using (EntryViewControl entryViewControl = CreateForm(apple, false))
			{
				Assert.IsTrue(entryViewControl.ControlFormattedView.Text.Contains("noun"));
				Assert.IsFalse(entryViewControl.ControlFormattedView.Text.Contains("nombre"));
			}
		}

		private void TestEntryShows(LexEntry entry)
		{
			using (EntryViewControl entryViewControl = CreateForm(entry, false))
			{
				Assert.IsTrue(
						entryViewControl.ControlFormattedView.Text.Contains(GetLexicalForm(entry)));
				Assert.IsTrue(entryViewControl.ControlFormattedView.Text.Contains(GetMeaning(entry)));
				Assert.IsTrue(
						entryViewControl.ControlFormattedView.Text.Contains(GetExampleSentence(entry)));
			}
		}

		[Test]
		[Ignore("For now, we also show the ghost field in this situation.")]
		public void EditField_SingleControl()
		{
			using (
					EntryViewControl entryViewControl = CreateFilteredForm(apple,
																		   _primaryMeaningFieldName,
																		   "LexSense",
																		   BasilProject.Project.
																				   WritingSystems.
																				   TestWritingSystemAnalId)
					)
			{
				Assert.AreEqual(1, entryViewControl.ControlEntryDetail.Count);
			}
		}

		[Test]
		public void EditField_SingleControlWithGhost()
		{
			using (
					EntryViewControl entryViewControl = CreateFilteredForm(apple,
																		   _primaryMeaningFieldName,
																		   "LexSense",
																		   BasilProject.Project.
																				   WritingSystems.
																				   TestWritingSystemAnalId)
					)
			{
				Assert.AreEqual(2, entryViewControl.ControlEntryDetail.Count);
			}
		}

		[Test]
		public void EditField_Change_UpdatesSenseMeaning()
		{
			EnsureField_Change_UpdatesSenseMeaning(car);
			EnsureField_Change_UpdatesSenseMeaning(bike);
		}

		private void EnsureField_Change_UpdatesSenseMeaning(LexEntry entry)
		{
			using (
					EntryViewControl entryViewControl = CreateFilteredForm(entry,
																		   _primaryMeaningFieldName,
																		   "LexSense",
																		   BasilProject.Project.
																				   WritingSystems.
																				   TestWritingSystemAnalId)
					)
			{
				DetailList entryDetailControl = entryViewControl.ControlEntryDetail;
				Label labelControl = entryDetailControl.GetLabelControlFromRow(0);
				Assert.AreEqual("Meaning 1", labelControl.Text);
				MultiTextControl editControl =
						(MultiTextControl) entryDetailControl.GetEditControlFromRow(0);
				editControl.TextBoxes[0].Focus();
				editControl.TextBoxes[0].Text = "test";
				entryDetailControl.GetEditControlFromRow(1).Focus();
				Assert.IsTrue(editControl.TextBoxes[0].Text.Contains(GetMeaning(entry)));
			}
		}

		[Test]
		[Ignore("RTF View is on its way out anyways")]
		public void EditField_Change_DisplayedInFormattedView()
		{
			using (
					EntryViewControl entryViewControl = CreateFilteredForm(apple,
																		   Field.FieldNames.
																				   EntryLexicalForm.
																				   ToString(),
																		   "LexEntry",
																		   BasilProject.Project.
																				   WritingSystems.
																				   TestWritingSystemVernId)
					)
			{
				DetailList entryDetailControl = entryViewControl.ControlEntryDetail;
				MultiTextControl editControl =
						(MultiTextControl) entryDetailControl.GetEditControlFromRow(0);
				editControl.TextBoxes[0].Text = "test";
				Assert.IsTrue(entryViewControl.ControlFormattedView.Text.Contains("test"));
			}
		}

		[Test]
		public void EditField_RemoveContents_RemovesSense()
		{
			LexEntry meaningOnly = CreateTestEntry("word", "meaning", "");
			using (EntryViewControl entryViewControl = CreateForm(meaningOnly, true))
			{
				MultiTextControl editControl = GetEditControl(entryViewControl.ControlEntryDetail,
															  "Meaning 1");
				editControl.TextBoxes[0].Focus();
				editControl.TextBoxes[0].Text = "";
				entryViewControl.ControlEntryDetail.GetEditControlFromRow(0).Focus();
				Application.DoEvents();
				Thread.Sleep(1000);
				Application.DoEvents();

				Assert.IsTrue(
						GetEditControl(entryViewControl.ControlEntryDetail, "Meaning").Name.Contains
								("ghost"),
						"Only ghost should remain");
			}
		}

		private static MultiTextControl GetEditControl(DetailList detailList, string labelText)
		{
			MultiTextControl editControl = null;
			for (int i = 0;i < detailList.Count;i++)
			{
				Label label = detailList.GetLabelControlFromRow(i);
				if (label.Text == labelText)
				{
					editControl = (MultiTextControl) detailList.GetEditControlFromRow(i);
					break;
				}
			}
			return editControl;
		}

		[Test]
		[Ignore("RTF View is on its way out anyways")]
		public void FormattedView_FocusInControl_Displayed()
		{
			using (
					EntryViewControl entryViewControl = CreateFilteredForm(apple,
																		   Field.FieldNames.
																				   EntryLexicalForm.
																				   ToString(),
																		   "LexEntry",
																		   BasilProject.Project.
																				   WritingSystems.
																				   TestWritingSystemVernId)
					)
			{
				entryViewControl.ControlFormattedView.Select();
				string rtfOriginal = entryViewControl.ControlFormattedView.Rtf;

				DetailList entryDetailControl = entryViewControl.ControlEntryDetail;
				Control editControl = entryDetailControl.GetEditControlFromRow(0);

				//JDH added after we added multiple ws's per field. Was: editControl.Select();
				((MultiTextControl) editControl).TextBoxes[0].Select();

				Assert.AreNotEqual(rtfOriginal, entryViewControl.ControlFormattedView.Rtf);
			}
		}

		[Test]
		[Ignore("Not implemented yet.")]
		public void DoSomethingSensibleWhenWSInFieldWasntListedInProjectCollection() {}

		[Test]
		[Ignore("RTF View is on its way out anyways")]
		public void FormattedView_ChangeRecordThenBack_NothingHighlighted()
		{
			using (
					EntryViewControl entryViewControl = CreateFilteredForm(apple,
																		   Field.FieldNames.
																				   EntryLexicalForm.
																				   ToString(),
																		   "LexEntry",
																		   BasilProject.Project.
																				   WritingSystems.
																				   TestWritingSystemVernId)
					)
			{
				entryViewControl.ControlFormattedView.Select();
				string rtfAppleNothingHighlighted = entryViewControl.ControlFormattedView.Rtf;

				DetailList entryDetailControl = entryViewControl.ControlEntryDetail;
				Control editControl = entryDetailControl.GetEditControlFromRow(0);

				//JDH added after we added multiple ws's per field. Was: editControl.Select();
				((MultiTextControl) editControl).TextBoxes[0].Select();

				Assert.AreNotEqual(rtfAppleNothingHighlighted,
								   entryViewControl.ControlFormattedView.Rtf);

				entryViewControl.DataSource = banana;
				entryViewControl.DataSource = apple;
				//            Debug.WriteLine("Expected: "+rtfAppleNothingHighlighted);
				//            Debug.WriteLine("Actual:" + lexFieldControl.ControlFormattedView.Rtf);
				Assert.AreEqual(rtfAppleNothingHighlighted,
								entryViewControl.ControlFormattedView.Rtf);
			}
		}

		[Test]
		[Ignore("RTF View is on its way out anyways")]
		public void FormattedView_EmptyField_StillHighlighted()
		{
			using (
					EntryViewControl entryViewControl = CreateFilteredForm(empty,
																		   Field.FieldNames.
																				   EntryLexicalForm.
																				   ToString(),
																		   "LexEntry",
																		   BasilProject.Project.
																				   WritingSystems.
																				   TestWritingSystemVernId)
					)
			{
				entryViewControl.ControlFormattedView.Select();
				string rtfEmptyNothingHighlighted = entryViewControl.ControlFormattedView.Rtf;

				DetailList entryDetailControl = entryViewControl.ControlEntryDetail;
				Control editControl = entryDetailControl.GetEditControlFromRow(0);

				//JDH added after we added multiple ws's per field. Was: editControl.Select();
				((MultiTextControl) editControl).TextBoxes[0].Select();

				Assert.AreNotEqual(rtfEmptyNothingHighlighted,
								   entryViewControl.ControlFormattedView.Rtf);
			}
		}

		private EntryViewControl CreateForm(LexEntry entry, bool requiresVisibleForm)
		{
			EntryViewControl entryViewControl = new EntryViewControl();
			entryViewControl.LexEntryRepository = _lexEntryRepository;
			entryViewControl.ViewTemplate = _viewTemplate;
			entryViewControl.DataSource = entry;

			if(requiresVisibleForm)
			{
				Form window = new Form();
				window.Controls.Add(entryViewControl);
				window.Show();
			}
			return entryViewControl;
		}

		private EntryViewControl CreateFilteredForm(LexEntry entry,
													string field,
													string className,
													params string[] writingSystems)
		{
			ViewTemplate viewTemplate = new ViewTemplate();
			viewTemplate.Add(new Field(field, className, writingSystems));
			EntryViewControl entryViewControl = new EntryViewControl();
			entryViewControl.LexEntryRepository = _lexEntryRepository;
			entryViewControl.ViewTemplate = viewTemplate;
			entryViewControl.DataSource = entry;


			Form window = new Form();
			window.Controls.Add(entryViewControl);
			window.Show();

			return entryViewControl;
		}

		private LexEntry CreateTestEntry(string lexicalForm, string meaning, string exampleSentence)
		{
			LexEntry entry = _lexEntryRepository.CreateItem();
			entry.LexicalForm[GetSomeValidWsIdForField("EntryLexicalForm")] = lexicalForm;
			LexSense sense = new LexSense();
			entry.Senses.Add(sense);
#if GlossMeaning
			sense.Gloss[GetSomeValidWsIdForField("SenseGloss")] = meaning;
#else
			sense.Definition[WeSayWordsProject.Project.WritingSystems.TestWritingSystemAnalId] =
					meaning;
#endif
			LexExampleSentence example = new LexExampleSentence();
			sense.ExampleSentences.Add(example);
			example.Sentence[GetSomeValidWsIdForField("ExampleSentence")] = exampleSentence;
			_lexEntryRepository.SaveItem(entry);
			return entry;
		}

		private static string GetSomeValidWsIdForField(string fieldName)
		{
			return
					WeSayWordsProject.Project.DefaultViewTemplate.GetField(fieldName).
							WritingSystemIds[0];
		}

		private static string GetLexicalForm(LexEntry entry)
		{
			return entry.LexicalForm.GetFirstAlternative();
		}

		private static string GetMeaning(LexEntry entry)
		{
#if GlossMeaning
			return entry.Senses[0].Gloss.GetFirstAlternative();
#else
			return entry.Senses[0].Definition.GetFirstAlternative();
#endif
		}

		private static string GetExampleSentence(LexEntry entry)
		{
			return entry.Senses[0].ExampleSentences[0].Sentence.GetFirstAlternative();
		}
	}
}