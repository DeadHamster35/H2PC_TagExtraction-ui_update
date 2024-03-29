/*
	BlamLib: .NET SDK for the Blam Engine

	See license\BlamLib\BlamLib for specific license information
*/
﻿using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BlamLib.Test
{
	[TestClass]
	public partial class Halo1 : BaseTestClass
	{
		[ClassInitialize]
		public static void Initialize(TestContext context)
		{
			Directory.CreateDirectory(kTestResultsPath);
			Directory.CreateDirectory(kTestResultsTagsPath);
		}
		[ClassCleanup]
		public static void Dispose()
		{
		}

		[TestMethod]
		public void Halo1TestTagIndex()
		{
			using (var handler = new TagIndexHandler<Managers.TagIndex>(BlamVersion.Halo1_CE, kTestTagIndexTagsPath))
			{
				var tagindex = handler.IndexInterface;
				Blam.DatumIndex datum_scnr_index, datum_matg_index;

				StartStopwatch();
				{
					StartSubStopwatch();
					datum_scnr_index = tagindex.Open(@"levels\test\tutorial\tutorial", Blam.Halo1.TagGroups.scnr, IO.ITagStreamFlags.LoadDependents);
					Console.WriteLine("SCENARIO LOAD: Time taken: {0}", StopSubStopwatch());
				}
 				{
					StartSubStopwatch();
					datum_matg_index = tagindex.Open(@"globals\globals", Blam.Halo1.TagGroups.matg, IO.ITagStreamFlags.LoadDependents);
					Console.WriteLine("GLOBALS LOAD: Time taken: {0}", StopSubStopwatch());
 				}
				Console.WriteLine("TAG INDEX: Time taken: {0}", StopStopwatch());

				Managers.TagManager tag_manager_scnr = tagindex[datum_scnr_index];
				var tag_scenario = tag_manager_scnr.TagDefinition as Blam.Halo1.Tags.scenario_group;
				tag_scenario = null;
				tag_manager_scnr = null;
// 				using (var sw = new StreamWriter(Path.Combine(kTestResultsTagsPath, @"dump_tag_index.txt")))
// 				{ tagindex.Dump(sw); }
				Assert.IsTrue(tagindex.Unload(datum_scnr_index));
				Assert.IsTrue(tagindex.Unload(datum_matg_index));
			}
		}

		[TestMethod]
		public void Halo1TestTagIndexRar()
		{
			using (var handler = new TagIndexHandler<Managers.TagIndex>(BlamVersion.Halo1_CE, kTestTagIndexTagsPath))
			{
				var tagindex = handler.IndexInterface;
				Blam.DatumIndex datum_test_index;

				{
					StartStopwatch();
					datum_test_index = tagindex.Open(
						@"characters\cyborg\cyborg", Blam.Halo1.TagGroups.bipd,
						IO.ITagStreamFlags.LoadDependents);
					Console.WriteLine("TEST LOAD: Time taken: {0}", StopStopwatch());
				}
				tagindex.WinRar(Path.Combine(kTestResultsTagsPath, @"test.rar"), true);
				Assert.IsTrue(tagindex.Unload(datum_test_index));
			}
		}

		[TestMethod]
		public void Halo1TestTagIndexThreaded()
		{
			var bd = BlamLib.Program.Halo1.Manager;
			
			var thread_matg = new System.Threading.Thread(delegate (/*object param*/)
			{
				StartStopwatch();
				using (var handler = new TagIndexHandler<Managers.TagIndex>(BlamVersion.Halo1_CE, kTestTagIndexTagsPath))
				{
					var tagindex_matg = handler.IndexInterface;

					var datum_test_index = tagindex_matg.Open(
						@"globals\globals", Blam.Halo1.TagGroups.matg,
						IO.ITagStreamFlags.LoadDependents);
					Console.WriteLine("GLOBALS LOAD: Time taken: {0}", StopStopwatch());

				}
			});
			var thread_scnr = new System.Threading.Thread(delegate(/*object param*/)
			{
				StartStopwatch();
				using (var handler = new TagIndexHandler<Managers.TagIndex>(BlamVersion.Halo1_CE, kTestTagIndexTagsPath))
				{
					var tagindex_scnr = handler.IndexInterface;

					var datum_test_index2 = tagindex_scnr.Open(
						@"levels\test\tutorial\tutorial", Blam.Halo1.TagGroups.scnr,
						IO.ITagStreamFlags.LoadDependents);
					Console.WriteLine("SCENARIO LOAD: Time taken: {0}", StopStopwatch());
				}
			});

			thread_matg.Start(/*TestContext*/);
			thread_scnr.Start(/*TestContext*/);
			thread_matg.Join();
			thread_scnr.Join();
		}

		[TestMethod]
		public void Halo1TestTagIndexSharing()
		{
			using (var handler_matg = new TagIndexHandler<Managers.TagIndex>(BlamVersion.Halo1_CE, kTestTagIndexTagsPath))
			using (var handler_scnr = new TagIndexHandler<Managers.TagIndex>(BlamVersion.Halo1_CE, kTestTagIndexTagsPath))
			{
				var tagindex_matg = handler_matg.IndexInterface;
				var tagindex_scnr = handler_scnr.IndexInterface;

				Blam.DatumIndex datum_test_index, datum_test_index2;

				{
					StartStopwatch();
					datum_test_index = tagindex_matg.Open(
						@"globals\globals", Blam.Halo1.TagGroups.matg,
						IO.ITagStreamFlags.LoadDependents);
					Console.WriteLine("TEST LOAD: Time taken: {0}", StopStopwatch());
				}
				{
					StartStopwatch();
					datum_test_index2 = tagindex_scnr.Open(
						@"levels\test\tutorial\tutorial", Blam.Halo1.TagGroups.scnr,
						IO.ITagStreamFlags.LoadDependents);
					Console.WriteLine("TEST LOAD 2: Time taken: {0}", StopStopwatch());
				}

				//using (var sw = new StreamWriter(Path.Combine(kTestResultsTagsPath, @"dump_shared_tag_index.txt")))
				//{ tagindex_scnr.DumpSharedReferences(sw, tagindex_matg); }

				tagindex_matg.ToDatabase(Path.Combine(kTestResultsTagsPath, @"_matg.tag_database"));
				tagindex_scnr.ToDatabase(Path.Combine(kTestResultsTagsPath, @"_scnr.tag_database"));

				Assert.IsTrue(tagindex_scnr.Unload(datum_test_index2));
				Assert.IsTrue(tagindex_matg.Unload(datum_test_index));
			}
		}

		[TestMethod]
		public void Halo1TestTagIndexSharingThreaded()
		{
			StartStopwatch();

			var bd = BlamLib.Program.Halo1.Manager;

			TagIndexHandler<Managers.TagIndex> handler_matg = null;
			var thread_matg = new System.Threading.Thread(delegate (/*object param*/)
			{
				StartSubStopwatch();
				handler_matg = new TagIndexHandler<Managers.TagIndex>(BlamVersion.Halo1_CE, kTestTagIndexTagsPath);
				{
					var tagindex_matg = handler_matg.IndexInterface;

					var datum_test_index = tagindex_matg.Open(
						@"globals\globals", Blam.Halo1.TagGroups.matg,
						IO.ITagStreamFlags.LoadDependents);

					tagindex_matg.Open(@"ui\ui_tags_loaded_all_scenario_types", Blam.Halo1.TagGroups.tagc,
						IO.ITagStreamFlags.LoadDependents);

					Console.WriteLine("GLOBALS LOAD: Time taken: {0}", StopSubStopwatch());
				}
			});

			TagIndexHandler<Managers.TagIndex> handler_scnr = null;
			var thread_scnr = new System.Threading.Thread(delegate(/*object param*/)
			{
				StartSubStopwatch();
				handler_scnr = new TagIndexHandler<Managers.TagIndex>(BlamVersion.Halo1_CE, kTestTagIndexTagsPath);
				{
					var tagindex_scnr = handler_scnr.IndexInterface;

					var datum_test_index2 = tagindex_scnr.Open(
						@"levels\test\tutorial\tutorial", Blam.Halo1.TagGroups.scnr,
						IO.ITagStreamFlags.LoadDependents);

					tagindex_scnr.Open(@"ui\ui_tags_loaded_multiplayer_scenario_type", Blam.Halo1.TagGroups.tagc,
						IO.ITagStreamFlags.LoadDependents);

					Console.WriteLine("SCENARIO LOAD: Time taken: {0}", StopSubStopwatch());
				}
			});

			thread_matg.Start(/*TestContext*/);
			thread_scnr.Start(/*TestContext*/);
			thread_matg.Join();
			thread_scnr.Join();

			using (var sw = new StreamWriter(Path.Combine(kTestResultsTagsPath, @"dump_tag_index_matg.txt")))
			{ handler_matg.IndexInterface.Dump(sw); }
			using (var sw = new StreamWriter(Path.Combine(kTestResultsTagsPath, @"dump_tag_index_sncr.txt")))
			{ handler_scnr.IndexInterface.Dump(sw); }
			using (var sw = new StreamWriter(Path.Combine(kTestResultsTagsPath, @"dump_shared_tag_index.txt")))
			{ handler_scnr.IndexInterface.DumpSharedReferences(sw, handler_matg.IndexInterface); }

			handler_matg.Dispose();
			handler_scnr.Dispose();

			Console.WriteLine("TOTAL: Time taken: {0}", StopStopwatch());
		}

		[TestMethod]
		public void Halo1TestTagIndexNonTags()
		{
			using (var handler = new TagIndexHandler<Managers.TagIndex>(BlamVersion.Halo1_CE, kTestTagIndexTagsPath))
			{
				var tagindex = handler.IndexInterface;

				using (var sw = new StreamWriter(Path.Combine(kTestResultsTagsPath, @"non_tag_files.txt")))
				{ tagindex.DumpNonTagFiles(sw); }
			}
		}

		#region COLLADA tests
		static readonly ModelTestDefinition[] ModelTestDefinitions = new ModelTestDefinition[]
		{
			new ModelTestDefinition("BIPED", @"characters\elite\elite",
			    Blam.Halo1.TagGroups.bipd),
			new ModelTestDefinition("BIPED", @"characters\cyborg\cyborg",
			    Blam.Halo1.TagGroups.bipd),
			new ModelTestDefinition("VEHICLE", @"vehicles\pelican\pelican",
			    Blam.Halo1.TagGroups.vehi),
			new ModelTestDefinition("VEHICLE", @"vehicles\warthog\warthog",
			    Blam.Halo1.TagGroups.vehi),
			new ModelTestDefinition("DEVICE MACHINE", @"levels\b30\devices\doors\door small\door small",
			    Blam.Halo1.TagGroups.mach),
			new ModelTestDefinition("SCENERY", @"scenery\tubewire\tubewire",
			    Blam.Halo1.TagGroups.scen),
			new ModelTestDefinition("DEVICE CONTROL", @"levels\a50\devices\interior tech objects\holo command control\holo command control",
			    Blam.Halo1.TagGroups.ctrl),
			new ModelTestDefinition("DEVICE LIGHT FIXTURE", @"levels\c10\devices\ground_bright\ground_bright",
			    Blam.Halo1.TagGroups.lifi),
			new ModelTestDefinition("GARBAGE", @"characters\sentinel\sentinel arm bit\sentinel arm bit",
			    Blam.Halo1.TagGroups.garb),
			new ModelTestDefinition("WEAPON", @"weapons\sniper rifle\sniper rifle",
			    Blam.Halo1.TagGroups.weap),
		};
		static readonly ModelTestDefinition[] BSPTestDefinitions = new ModelTestDefinition[]
		{
			new ModelTestDefinition("BSP", @"levels\test\ratrace\ratrace",
			    Blam.Halo1.TagGroups.sbsp),
			new ModelTestDefinition("BSP", @"levels\test\deathisland\deathisland",
			    Blam.Halo1.TagGroups.sbsp),
			new ModelTestDefinition("BSP", @"levels\a50\a50_exterior",
				Blam.Halo1.TagGroups.sbsp),
			new ModelTestDefinition("BSP", @"levels\b40\b40_b4",
				Blam.Halo1.TagGroups.sbsp),
		};

		[TestMethod]
		public void Halo1TestCOLLADAModelExport()
		{
			using (var handler = new TagIndexHandler<Managers.TagIndex>(BlamVersion.Halo1_CE, kTestTagIndexTagsPath))
			{
				var tagindex = handler.IndexInterface;
				foreach (var model_def in ModelTestDefinitions)
				{
					// load the tag with dependents
					StartStopwatch();
					{
						model_def.Open(tagindex);
						Console.WriteLine("{0} LOAD: Time taken: {1}", model_def.TypeString, m_testStopwatch.Elapsed);
					}
					Console.WriteLine("TAG INDEX: Time taken: {0}", StopStopwatch());

					// create a halo1 collada interface with the gbxmodel datum
					var object_tag = tagindex[model_def.TagIndex].TagDefinition as Blam.Halo1.Tags.object_group;

					var halo1 = new BlamLib.Render.COLLADA.Halo1.ColladaHalo1(tagindex, object_tag.Model.Datum);

					// set the interfaces settings (want to look into a better way of doing this)
					halo1.Overwrite = true;
					halo1.RelativeFilePath = Path.Combine(kTestTagIndexTagsPath, @"data\");

					// export each permutation and lod separately
					foreach (var info in halo1)
					{
						// clear the export register
						halo1.ClearRegister();
						// add the info object to export
						halo1.RegisterForExport(info);
						
						var model_info = info as Render.COLLADA.Halo1.ColladaHalo1ModelInfo;

						string name = string.Format("{0}-perm{1}-lod{2}", model_info.Name, model_info.Permutation, model_info.LevelOfDetail);

						StartStopwatch();
						halo1.Export(name);
						Console.WriteLine("EXPORT {0} TIME: Time taken: {1}", name, StopStopwatch());

						// print any errors
						foreach (string report in halo1.Reports())
							Console.WriteLine(report);
					}

					// export all the models into a single collada file
					halo1.ClearRegister();
					foreach (var info in halo1)
						halo1.RegisterForExport(info);

					string name_all = Path.GetFileNameWithoutExtension(model_def.Name) + "_all";

					StartStopwatch();
					halo1.Export(name_all);
					Console.WriteLine("EXPORT {0} TIME: Time taken: {1}", name_all, StopStopwatch());

					foreach (string report in halo1.Reports())
						Console.WriteLine(report);

					model_def.Close(tagindex);
				}
			}
		}

		[TestMethod]
		public void Halo1TestCOLLADABSPExport()
		{
			using (var handler = new TagIndexHandler<Managers.TagIndex>(BlamVersion.Halo1_CE, kTestTagIndexTagsPath))
			{
				var tagindex = handler.IndexInterface;
				foreach (var model_def in BSPTestDefinitions)
				{
					StartStopwatch();
					{
						model_def.Open(tagindex);
						Console.WriteLine(model_def.TypeString + " LOAD: Time taken: {0}", m_testStopwatch.Elapsed);
					}
					Console.WriteLine("TAG INDEX: Time taken: {0}", StopStopwatch());

					var halo1 = new BlamLib.Render.COLLADA.Halo1.ColladaHalo1(tagindex, model_def.TagIndex);

					halo1.Overwrite = true;
					halo1.RelativeFilePath = Path.Combine(kTestTagIndexTagsPath, @"data\");

					halo1.ClearRegister();

					foreach (var info in halo1)
						halo1.RegisterForExport(info);

					var bsp_info = halo1[0] as Render.COLLADA.Halo1.ColladaHalo1BSPInfo;

					StartStopwatch();
					halo1.Export(bsp_info.Name);
					Console.WriteLine("EXPORT {0} TIME: Time taken: {1}", bsp_info.Name, StopStopwatch());

					foreach (string report in halo1.Reports())
						Console.WriteLine(report);

					model_def.Close(tagindex);
				}
			}
		}

		[TestMethod]
		public void Halo1TestCOLLADAModelImport()
		{
			// this isn't importing collada files to gbxmodels or anything, just loading the collada file and validating it
			// so that we know the exported file can be imported without error

			var serializer = new System.Xml.Serialization.XmlSerializer(typeof(Render.COLLADA.ColladaFile));

			foreach (var model_def in ModelTestDefinitions)
			{
				string file_path = Path.Combine(kTestTagIndexTagsPath, @"data\");
				file_path = Path.Combine(file_path, Path.GetFileNameWithoutExtension(model_def.Name) + "_all.dae");
				
				using (var reader = new System.Xml.XmlTextReader(file_path))
				{
					var collada_file = serializer.Deserialize(reader) as Render.COLLADA.ColladaFile;

					var validator = new BlamLib.Render.COLLADA.Validation.ColladaFileValidator();

					validator.ErrorOccured += new EventHandler<BlamLib.Render.COLLADA.ColladaErrorEventArgs>(ValidatorErrorOccured);
					bool success = validator.ValidateFile(collada_file);
					validator.ErrorOccured -= new EventHandler<BlamLib.Render.COLLADA.ColladaErrorEventArgs>(ValidatorErrorOccured);

					if (!success)
						Assert.Fail("COLLADA file failed validation");
				}
			}
		}

		void ValidatorErrorOccured(object sender, BlamLib.Render.COLLADA.ColladaErrorEventArgs e)
		{
			Console.WriteLine(e.ErrorMessage);
		}
		#endregion
	};
}