/*
	BlamLib: .NET SDK for the Blam Engine

	See license\BlamLib\BlamLib for specific license information
*/
using System;
using System.Collections.Generic;
using System.Text;

namespace BlamLib.Blam.Halo1.CheApe
{
	public sealed class Compiler : BlamLib.CheApe.Compiler
	{
		class DataArray : IO.IStreamable
		{
			#region Constants
			public const int MaxValue = 64;
			public const int Size = 0x38;
			public const int TotalSize = (DataArray.MaxValue * Item.Size) + DataArray.Size;
			#endregion

			#region Items
			public class Item : IO.IStreamable
			{
				public const int Size = 2 + 2 + 4;

				public short Header = 0;
				public short Flags = 0;
				public uint Address = 0;

				#region IStreamable Members
				public void Read(IO.EndianReader stream) { throw new Exception("The method or operation is not implemented."); }

				public void Write(IO.EndianWriter stream)
				{
					stream.Write(Header);
					stream.Write(Flags);
					stream.Write(Address);
				}
				#endregion
			};
			public List<Item> Datums = new List<Item>();
			#endregion

			#region IStreamable Members
			public void Read(IO.EndianReader stream) { throw new Exception("The method or operation is not implemented."); }

			public void Write(IO.EndianWriter stream)
			{
				Compiler comp = stream.Owner as Compiler;
				Import import = comp.OwnerState.Importer as Import;

				int real_count = import.Groups.Count;
				stream.Write("dynamic tag groups", false);
				stream.Write((short)DataArray.MaxValue);
				stream.Write((short)Item.Size);
				stream.Write(true);
				stream.Write(true);
				stream.Write((short)0);
				MiscGroups.data.Write(stream);
				stream.Write((short)real_count);//stream.Write((short)Datums.Count);
				stream.Write((short)real_count);//stream.Write((short)Datums.Count);
				stream.Write(real_count);
				stream.WritePointer(stream.PositionUnsigned + 4);

				#region Write tag group datums
				foreach (Import.TagGroup tg in import.Groups.Values)
				{
					stream.Write((short)0); // Header
					stream.Write((short)0); // Flags
					comp.AddLocationFixup(tg.Name, stream, false);
					stream.Write((int)0);
				}
				#endregion

				#region Write null datums
				Item i = new Item();
				int count = DataArray.MaxValue - real_count;
				for (int x = 0; x < count; x++)
					i.Write(stream);
				#endregion
			}
			#endregion
		};

		#region Tag Interface
		#region Type Indicies
		internal int
			TypeIndexEnum = -1,
			TypeIndexLongFlags = -1,
			TypeIndexWordFlags = -1,
			TypeIndexByteFlags = -1,
			TypeIndexTagReference = -1,
			TypeIndexData = -1,
			TypeIndexBlock = -1,
			TypeIndexShortBlockIndex = -1,
			TypeIndexLongBlockIndex = -1,
			TypeIndexPad = -1,
			TypeIndexSkip = -1,
			TypeIndexArrayStart = -1,
			TypeIndexArrayEnd = -1,
			TypeIndexExplanation = -1,
			TypeIndexCustom = -1,
			TypeIndexTerminator = -1,
			TypeIndexStruct = -1,
			TypeIndexStringId = -1;

		private void InitializeTypeIndicies()
		{
			TypeIndexEnum = OwnerState.Definition.GetTypeIndex("Enum");
			TypeIndexLongFlags = OwnerState.Definition.GetTypeIndex("LongFlags");
			TypeIndexWordFlags = OwnerState.Definition.GetTypeIndex("WordFlags");
			TypeIndexByteFlags = OwnerState.Definition.GetTypeIndex("ByteFlags");
			TypeIndexTagReference = OwnerState.Definition.GetTypeIndex("TagReference");
			TypeIndexData = OwnerState.Definition.GetTypeIndex("Data");
			TypeIndexBlock = OwnerState.Definition.GetTypeIndex("Block");
			TypeIndexShortBlockIndex = OwnerState.Definition.GetTypeIndex("ShortBlockIndex");
			TypeIndexLongBlockIndex = OwnerState.Definition.GetTypeIndex("LongBlockIndex");
			TypeIndexPad = OwnerState.Definition.GetTypeIndex("Pad");
			TypeIndexSkip = OwnerState.Definition.GetTypeIndex("Skip");
			TypeIndexArrayStart = OwnerState.Definition.GetTypeIndex("ArrayStart");
			TypeIndexArrayEnd = OwnerState.Definition.GetTypeIndex("ArrayEnd");
			TypeIndexExplanation = OwnerState.Definition.GetTypeIndex("Explanation");
			TypeIndexCustom = OwnerState.Definition.GetTypeIndex("Custom");
			TypeIndexTerminator = OwnerState.Definition.GetTypeIndex("Terminator");
			TypeIndexStruct = OwnerState.Definition.GetTypeIndex("Struct");
			TypeIndexStringId = OwnerState.Definition.GetTypeIndex("StringId");
		}
		#endregion

		/// <summary>
		/// short field_type;
		/// short pad;
		/// cstring name;
		/// void* definition;
		/// </summary>
		internal sealed class Field : Object, Compiler.IField
		{
			public const int Size = 2 + 2 + 4 + 4;

			Import.Field field = null;
			public void Reset(Import.Field def) { field = def; }
			public Field() { }
			public Field(Import.Field def)	{ field = def; }

			#region IStreamable Members
			public override void Write(IO.EndianWriter stream)
			{
				Compiler comp = stream.Owner as Compiler;

				#region TypeIndex
				if (field.TypeIndex == comp.TypeIndexStringId)
					stream.Write(comp.TypeIndexTagReference);
				else
					stream.Write(field.TypeIndex);
				#endregion

				if (field.TypeIndex != comp.TypeIndexPad)
					stream.Write(field.Name);
				else
					stream.Write((int)0);

				if (field.TypeIndex == comp.TypeIndexArrayStart ||
					field.TypeIndex == comp.TypeIndexPad ||
					field.TypeIndex == comp.TypeIndexSkip)
					stream.Write(field.ToInt());
				else if (field.TypeIndex == comp.TypeIndexCustom)
					stream.WriteTag(field.ToString());
				else if (field.TypeIndex == comp.TypeIndexExplanation)
				{
					if (field.Definition != string.Empty)
						stream.Write(field.ToPointer());
					else
						stream.Write(comp.Strings.GetNull());
				}
				else if (	field.TypeIndex == comp.TypeIndexByteFlags ||
							field.TypeIndex == comp.TypeIndexEnum ||
							field.TypeIndex == comp.TypeIndexWordFlags ||
							field.TypeIndex == comp.TypeIndexShortBlockIndex ||
							field.TypeIndex == comp.TypeIndexLongFlags ||
							field.TypeIndex == comp.TypeIndexLongBlockIndex ||
							field.TypeIndex == comp.TypeIndexTagReference ||
							field.TypeIndex == comp.TypeIndexData ||
							field.TypeIndex == comp.TypeIndexBlock || 
					
							field.TypeIndex == comp.TypeIndexStringId)
				{
					comp.AddLocationFixup(field.Definition, stream);

					stream.Write((int)0); // should come back later and write the address
				}
				else
					stream.Write((int)0);
			}
			#endregion
		};

		internal override IField ConstructField(Import.Field def)
		{
			return new Field(def);
		}

		struct FieldsWriter
		{
			List<Import.Field> mFields;

			public FieldsWriter(List<Import.Field> fields) { mFields = fields; }

			static void WriteFieldsRecursive(IO.EndianWriter stream, Compiler comp, List<Import.Field> fields)
			{
				Field temp = new Field();
				foreach (Import.Field f in fields)
				{
					if (f.TypeIndex == comp.TypeIndexStruct)
					{
						var import = comp.OwnerState.Importer as Import;
						Import.TagStruct ts;
						// Assert that the field references a valid definition
						if (import.Structs.TryGetValue(f.Definition, out ts))
							WriteFieldsRecursive(stream, comp, ts.Fields.Fields);
						else
							Debug.Assert.If(false, "Field '{0}' references an undefined tag struct '{1}'",
								comp.Strings[f.Name], f.ToString());
					}
					else
					{
						// StringId support is a hack based on tag_reference fields
						// We internally define the tag reference definition, so we must internally add it
						if (f.TypeIndex == comp.TypeIndexStringId)
							f.ChangeDefinition(Import.kStringIdFieldDefinitionName);

						temp.Reset(f);
						temp.Write(stream);

						// While the initial part of the StringId hack is based on a tag_reference field,
						// we use an additional 32 bits for the string_id handle (instead of playing with
						// the tag_reference's tag_index member). This is seen as padding by the editor
						if (f.TypeIndex == comp.TypeIndexStringId)
						{
							temp.Reset((comp.OwnerState.Importer as Halo1.CheApe.Import).StringIdFieldHandlePadding);
							temp.Write(stream);
						}
					}
				}
			}
			public uint WriteFields(IO.EndianWriter stream, Compiler comp)
			{
				uint fieldsAddress = stream.PositionUnsigned;

				WriteFieldsRecursive(stream, comp, mFields);

				stream.Write(comp.TypeIndexTerminator); stream.Write((int)0); stream.Write((int)0);
				return fieldsAddress;
			}
		}

		/// <summary>
		/// string name;
		/// long flags;
		/// long max_elements;
		/// long element_size;
		/// long pad;
		/// void* fields;
		/// void* proc_add;
		/// void* proc_postprocess;
		/// void* format;
		/// void* delete;
		/// long* byteswap_codes;
		/// </summary>
		internal sealed class TagBlock : Object
		{
			public const int Size = 4 * 11;

			Import.TagBlock tagBlock = null;
			public void Reset(Import.TagBlock def) { tagBlock = def; }
			public TagBlock() { }
			public TagBlock(Import.TagBlock def)	{ tagBlock = def; }

			#region IStreamable Members
			public override void Write(IO.EndianWriter stream)
			{
				Compiler comp = stream.Owner as Compiler;

				uint fieldsAddress = new FieldsWriter(tagBlock.Fields).WriteFields(stream, comp);
				comp.MarkLocationFixup(tagBlock.Name, stream, false);

				int flags = (
						(tagBlock.DontReadChildren ? 1 << 0 : 0)
					);

				stream.Write(tagBlock.Name);
				stream.Write(flags);
				stream.Write(tagBlock.MaxElements);
				stream.Write(tagBlock.CalculateSize(comp.OwnerState));
				stream.Write((int)0);
				stream.WritePointer(fieldsAddress);

				// procs
				stream.Write((int)0); stream.Write((int)0); stream.Write((int)0); stream.Write((int)0);

				stream.Write((int)0); // byte swap codes address
			}
			#endregion
		};

		/// <summary>
		/// cstring name;
		/// long flags;
		/// tag group_tag;
		/// tag parent_tag;
		/// short version;
		/// short pad;
		/// void* proc_postprocess;
		/// void* header_block_definition; 
		/// tag child_group_tags[16]; 
		/// short child_count;
		/// pad16;
		/// pad32;
		/// </summary>
		internal sealed class TagGroup : Object
		{
			public const int Size = (4 * 4) + (2 + 2) + (4 * 2) +
				((4 * 16) + 2 + 2) + 4;

			Import.TagGroup tagGroup = null;
			public void Reset(Import.TagGroup def) { tagGroup = def; }
			public TagGroup() { }
			public TagGroup(Import.TagGroup def)	{ tagGroup = def; }

			#region IStreamable Members
			public override void Write(IO.EndianWriter stream)
			{
				Compiler comp = stream.Owner as Compiler;

				int flags;

				#region Block
				var tag_block = tagGroup.Block as Import.TagBlock;
				uint fieldsAddress = new FieldsWriter(tag_block.Fields).WriteFields(stream, comp);
				//comp.MarkLocationFixup(tag_block.Name, stream, false);

				flags = (
						(tag_block.DontReadChildren ? 1 << 0 : 0)
					);

				uint blockAddress = stream.PositionUnsigned;
				stream.Write(tagGroup.BlockName);
				stream.Write(flags);
				stream.Write((int)1); // max elements
				stream.Write(tag_block.CalculateSize(comp.OwnerState));
				stream.Write((int)0);
				stream.WritePointer(fieldsAddress);

				// procs
				stream.Write((int)0); stream.Write((int)0); stream.Write((int)0); stream.Write((int)0);

				stream.Write((int)0); // byte swap codes address
				#endregion

				comp.MarkLocationFixup(tagGroup.Name, stream, false);

				flags = (
						(tagGroup.IsIncludedInTagGroupsChecksum ? 1 << 0 : 0)
					);

				stream.Write(tagGroup.Name);
				stream.Write(flags);
				if (string.IsNullOrEmpty(tagGroup.GroupTag))
					Debug.LogFile.WriteLine("CheApe: tag_group '{0}' has a bad group-tag...check your XML?");
				stream.WriteTag(tagGroup.GroupTag);
				if (tagGroup.ParentTag != null)
				{
					if (string.IsNullOrEmpty(tagGroup.GroupTag))
						Debug.LogFile.WriteLine("CheApe: tag_group '{0}' has a bad parent group-tag...check your XML?");
					stream.WriteTag(tagGroup.ParentTag);
				}
				else
					stream.Write((int)-1);
				stream.Write(tagGroup.Version); stream.Write((short)0);
				stream.Write((int)0); // post process proc
				stream.WritePointer(blockAddress);
				for (int x = 0; x < 17; x++) stream.Write((int)0); // child group tags
				stream.Write((int)0); // we don't support that shit, gtfo
				stream.Write((int)0);
			}
			#endregion
		};
		#endregion

		#region Script Interface
		/// <summary>
		/// short return_type;
		/// pad16;
		/// cstring name;
		/// void* parse;
		/// void* evaluate;
		/// cstring info;
		/// cstring param_info;
		/// short access;
		/// short argc;
		/// short args[]; (pad16 if odd count)
		/// </summary>
		internal sealed class ScriptFunction : Object
		{
			public const int Size = 2 + 2 + 4 + 4 + 4 + 4 + 4 + 2 + 2 + 2
				+ 2;

			Import.ScriptFunction func = null;
			public void Reset(Import.ScriptFunction def) { func = def; }
			public ScriptFunction() { }
			public ScriptFunction(Import.ScriptFunction def) { func = def; }

			#region IStreamable Members
			public override void Write(IO.EndianWriter stream)
			{
				Compiler comp = stream.Owner as Compiler;

				comp.MarkLocationFixup(func.Name, stream, false);
				stream.Write(func.returnType.Opcode);
				stream.Write((short)(func.IsInternal ? 1 : 0));//stream.Write(ushort.MinValue);
				stream.Write(func.Name);
				stream.Write(uint.MaxValue);
				stream.Write(uint.MaxValue);
				stream.Write(func.Help);
				stream.Write(func.HelpArg);
				stream.Write(ushort.MinValue);
				short count; stream.Write(count = (short)func.Arguments.Count);
				foreach (Import.ScriptFunctionArg arg in func.Arguments) stream.Write(arg.type.Opcode);
				if ((count % 2) > 0) stream.Write(ushort.MinValue);
			}
			#endregion
		};

		/// <summary>
		/// cstring name;
		/// short type;
		/// pad16;
		/// void* address;
		/// short access;
		/// pad16;
		/// </summary>
		internal sealed class ScriptGlobal : Object
		{
			public const int Size = 4 + 2 + 2 + 4 + 2 + 2;

			Import.ScriptGlobal glob = null;
			public void Reset(Import.ScriptGlobal def) { glob = def; }
			public ScriptGlobal() { }
			public ScriptGlobal(Import.ScriptGlobal def) { glob = def; }

			#region IStreamable Members
			public override void Write(IO.EndianWriter stream)
			{
				Compiler comp = stream.Owner as Compiler;

				comp.MarkLocationFixup(glob.Name, stream, false);
				stream.Write(glob.Name);
				stream.Write(glob.type.Opcode);
				stream.Write((short)(glob.IsInternal ? 1 : 0));//stream.Write(ushort.MinValue);
				stream.Write(uint.MaxValue);
				stream.Write(uint.MinValue);
			}
			#endregion
		};


		void ScriptingHeadersToStream(Import import)
		{
			string name;
			uint base_address = OwnerState.Definition.MemoryInfo.BaseAddress;

			if ((Head.ScriptFunctionsCount = import.ScriptFunctions.Count) > 0)
			{
				Head.ScriptFunctionsAddress = base_address + MemoryStream.PositionUnsigned;
				// allocate script functions pointers
				foreach (Import.ScriptFunction sc in import.ScriptFunctions.Values)
				{
					name = sc.ToString();
					AddLocationFixup(name, MemoryStream);
					MemoryStream.Write(uint.MinValue);
				}

				AlignMemoryStream(Compiler.kDefaultAlignment);
			}

			if ((Head.ScriptGlobalsCount = import.ScriptGlobals.Count) > 0)
			{
				Head.ScriptGlobalsAddress = base_address + MemoryStream.PositionUnsigned;
				// allocate script global pointers
				foreach (Import.ScriptGlobal sc in import.ScriptGlobals.Values)
				{
					name = sc.ToString();
					AddLocationFixup(name, MemoryStream);
					MemoryStream.Write(uint.MinValue);
				}

				AlignMemoryStream(Compiler.kDefaultAlignment);
			}
		}

		void ScriptingDefinitionsToStream(Import import)
		{
			if (Head.ScriptFunctionsCount > 0)
			{
				ScriptFunction sfunc = new ScriptFunction();
				foreach (Import.ScriptFunction sc in import.ScriptFunctions.Values)
				{
					sfunc.Reset(sc);
					sfunc.Write(MemoryStream);
				}
			}

			if (Head.ScriptGlobalsCount > 0)
			{
				ScriptGlobal sglob = new ScriptGlobal();
				foreach (Import.ScriptGlobal sc in import.ScriptGlobals.Values)
				{
					sglob.Reset(sc);
					sglob.Write(MemoryStream);
				}
			}
		}

		void ScriptingToStream(Import import)
		{
			ScriptingHeadersToStream(import);
			ScriptingDefinitionsToStream(import);
		}
		#endregion


		DataArray DynamicTagGroups;

		uint CalculateStringPoolBaseAddress()
		{
			return OwnerState.Definition.MemoryInfo.BaseAddress + DataArray.TotalSize + 8; // we pad by 8 bytes in the cache after the data array, so + 8
		}

		internal Compiler(Project.ProjectState state) : base(state.Engine)
		{
			OwnerState = state;

			DynamicTagGroups = new DataArray();
			Strings = new Util.StringPool(true, CalculateStringPoolBaseAddress());

			InitializeTypeIndicies();
		}

		public override void Reset()
		{
			base.Reset();

			DynamicTagGroups = new DataArray();
			Strings = new Util.StringPool(true, CalculateStringPoolBaseAddress());
		}

		public override void Write(IO.EndianWriter stream)
		{
			const int k_alignment = Compiler.kDefaultAlignment;
			int align;
			using (var mem = InitializeMemoryStream())
			{
				DynamicTagGroups.Write(MemoryStream); // write the data array to the stream
				MemoryStream.Write((int)0); MemoryStream.Write((int)0); // alignment

				StringPoolToMemoryStream();

				Import import = OwnerState.Importer as Import;

				FixupsToMemoryStream();

				ScriptingToStream(import);

				EnumerationsToMemoryStream();

				TagReferencesToMemoryStream();

				TagDataToMemoryStream();

				#region TagBlock
				TagBlock tb = new TagBlock();
				foreach (Import.TagBlock tagb in import.Blocks.Values)
				{
					tb.Reset(tagb);
					tb.Write(MemoryStream);
				}
				#endregion

				#region TagGroup
				TagGroup tg = new TagGroup();
				foreach (Import.TagGroup tagg in import.Groups.Values)
				{
					tg.Reset(tagg);
					tg.Write(MemoryStream);
				}
				#endregion

				PostprocessWritebacks();

				// Create header
				PostprocessHeaderThenStream(stream, CalculateStringPoolBaseAddress());

				mem.WriteTo(stream.BaseStream); // write all the data that will be read into memory from a tool to the file
			}

			align = k_alignment - (stream.Length % k_alignment);
			if (align != k_alignment) stream.Write(new byte[align]);
		}
	};
}