using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Addin.Transform.PdfDictionary;
using Palaso.DictionaryServices.Lift;
using Palaso.i18n;
using WeSay.AddinLib;
using WeSay.LexicalModel;
using WeSay.Project;

namespace Addin.Transform
{
  //don't show this anymore  [Extension]
	public class HtmlTransformer : LiftTransformer//todo remove this dependency
	{
		public override string LocalizedName
		{
			get { return StringCatalog.Get("~Export to HTML"); }
		}

		public override string LocalizedLabel
		{
			get { return LocalizedName; }
		}

		public override string LocalizedLongLabel
		{
			get { return LocalizedName; }
		}

		public override string Description
		{
			get
			{
				return
						StringCatalog.Get(
								"~Creates a simple Html version of the dictionary.  Not a very good way to go.");
			}
		}

		public override Image ButtonImage
		{
			get { return Resources.printButtonImage; }
		}

		public override Image DashboardButtonImage
		{
			get { return Resources.greenPrinter; }
		}

		public override string ID
		{
			get { return "ExportToHtml"; }
		}

		public override void Launch(Form parentForm, ProjectInfo projectInfo)
		{
			string pathToHtml = CreateFileToOpen(projectInfo, true, true);
			_pathToOutput = pathToHtml;

			string layoutCssPath = projectInfo.LocateFile(Path.Combine("templates", "defaultDictionary.css"));

			string destination =Path.Combine(Path.GetDirectoryName(pathToHtml), "defaultDictionary.css");

			File.Copy(layoutCssPath, destination, true);

			if (string.IsNullOrEmpty(pathToHtml))
			{
				return; // get this when the user cancels
			}
			if (_launchAfterTransform)
			{
				Process.Start(pathToHtml);
			}
		}

		protected string CreateFileToOpen(ProjectInfo projectInfo,
										  bool includeXmlDirective,
										  bool linkToUserCss)
		{
			LexEntryRepository lexEntryRepository = projectInfo.ServiceProvider.GetService(typeof(LexEntryRepository)) as LexEntryRepository;
			var pliftPath = Path.Combine(projectInfo.PathToExportDirectory, projectInfo.Name + ".plift");


				var maker = new PLiftMaker();
				maker.MakePLiftTempFile(pliftPath, lexEntryRepository,
										projectInfo.ServiceProvider.GetService(typeof(ViewTemplate)) as
										ViewTemplate, LiftWriter.ByteOrderStyle.NoBOM);


			var pathToOutput = Path.Combine(projectInfo.PathToExportDirectory,
											projectInfo.Name + ".html");
			if (File.Exists(pathToOutput))
			{
				File.Delete(pathToOutput);
			}

			var htmWriter = new FLExCompatibleXhtmlWriter(includeXmlDirective, linkToUserCss);
			using (var reader = new StreamReader(pliftPath))
			{
				using (var file = new StreamWriter(pathToOutput, false, new UTF8Encoding(false)))
				{
					htmWriter.Write(reader, file);
				}
			}
			return pathToOutput;
		}
	}
}