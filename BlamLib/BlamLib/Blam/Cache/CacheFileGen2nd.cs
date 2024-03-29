/*
	BlamLib: .NET SDK for the Blam Engine

	See license\BlamLib\BlamLib for specific license information
*/
using System.Collections.Generic;
using TI = BlamLib.TagInterface;

namespace BlamLib.Blam.Cache
{
	/// <summary>
	/// Cache Header interface for 2nd generation engines (namely for Xbox 1)
	/// </summary>
	public abstract class CacheHeaderGen2 : Blam.CacheHeader, ICacheHeaderStringId
	{
		#region SourceFile
		protected string sourceFile = "";
		/// <summary>
		/// Cache file path from the DVD
		/// </summary>
		public string SourceFile { get { return sourceFile; } }
		#endregion

		#region ICacheHeaderStringId
		protected int stringIdsCount;
		/// <summary>
		/// Total count of string ids
		/// </summary>
		public int StringIdsCount
		{
			get { return stringIdsCount; }
			set { stringIdsCount = value; }
		}

		protected int stringIdsBufferSize;
		/// <summary>
		/// Total size of the null terminated string id table
		/// </summary>
		public int StringIdsBufferSize
		{
			get { return stringIdsBufferSize; }
			set { stringIdsBufferSize = value; }
		}

		protected int stringIdIndicesOffset;
		/// <summary>
		/// Offsets of each string id in the null terminated string id table
		/// </summary>
		public int StringIdIndicesOffset
		{
			get { return stringIdIndicesOffset; }
			set { stringIdIndicesOffset = value; }
		}

		protected int stringIdsBufferOffset;
		/// <summary>
		/// Offset of the null terminated string id table
		/// </summary>
		public int StringIdsBufferOffset
		{
			get { return stringIdsBufferOffset; }
			set { stringIdsBufferOffset = value; }
		}
		#endregion

		#region ScenarioPath
		protected string scenarioPath;
		/// <summary>
		/// Tag path of the scenario
		/// </summary>
		public string ScenarioPath { get { return scenarioPath; } }
		#endregion

		#region NeedsShared
		protected bool needsShared;
		/// <summary>
		/// Cache requires shared cache files
		/// </summary>
		public bool NeedsShared	{ get { return needsShared; } }
		#endregion

		#region Tag Names
		protected int tagNamesCount;
		/// <summary>
		/// Number of tag names in the cache
		/// </summary>
		public int TagNamesCount
		{
			get { return tagNamesCount; }
			set { tagNamesCount = value; }
		}

		protected int tagNamesBufferOffset;
		/// <summary>
		/// Offset to the c-string buffer containing all the tag names
		/// </summary>
		public int TagNamesBufferOffset
		{
			get { return tagNamesBufferOffset; }
			set { tagNamesBufferOffset = value; }
		}

		protected int tagNamesBufferSize;
		/// <summary>
		/// Size of the c-string buffer which contains all the tag names
		/// </summary>
		public int TagNamesBufferSize
		{
			get { return tagNamesBufferSize; }
			set { tagNamesBufferSize = value; }
		}

		protected int tagNameIndicesOffset;
		/// <summary>
		/// Offset to a table containing the character index (relative to <b>TagNamesBuffer</b>) 
		/// marking the start of each tag name
		/// </summary>
		public int TagNameIndicesOffset
		{
			get { return tagNameIndicesOffset; }
			set { tagNameIndicesOffset = value; }
		}
		#endregion


		#region Checksum
		protected uint checksum;
		/// <summary>
		/// Cache's checksum (using a 32bit Xor algorithm, starting at after the cache header)
		/// </summary>
		public uint Checksum
		{
			get { return checksum; }
			set { checksum = value; }
		}
		#endregion
	};

	/// <summary>
	/// Cache Index interface for 2nd generation engines (namely for Xbox 1)
	/// </summary>
	public abstract class CacheIndexGen2 : Blam.CacheIndex
	{
		protected uint groupTagsOffset;
		protected uint groupTagsAddress;
		public uint GroupTagsAddress	{ get { return groupTagsAddress; } }

		protected int groupTagsCount;
		public int GroupTagsCount	{ get { return groupTagsCount; } }
	};


	public abstract class CacheItemGroupTagGen2 : IO.IStreamable
	{
		public TagInterface.TagGroup GroupTag1;
		public TagInterface.TagGroup GroupTag2;
		public TagInterface.TagGroup GroupTag3;

		public override string ToString()
		{
			return string.Format("{0}\t{1}\t{2}", 
				GroupTag1.TagToString(), 
				GroupTag2.TagToString(), 
				GroupTag3.TagToString()); }

		#region IStreamable Members
		protected void ReadGroupTags(Managers.BlamDefinition gd, IO.EndianReader s)
		{
			uint gt = s.ReadUInt32();
			if (gt != uint.MaxValue)	GroupTag1 = gd.TagGroupFind(TagInterface.TagGroup.FromUInt(gt));
			else						GroupTag1 = TagInterface.TagGroup.Null;

			gt = s.ReadUInt32();
			if (gt != uint.MaxValue)	GroupTag2 = gd.TagGroupFind(TagInterface.TagGroup.FromUInt(gt));
			else						GroupTag2 = TagInterface.TagGroup.Null;

			gt = s.ReadUInt32();
			if (gt != uint.MaxValue)	GroupTag3 = gd.TagGroupFind(TagInterface.TagGroup.FromUInt(gt));
			else						GroupTag3 = TagInterface.TagGroup.Null;
		}

		public abstract void Read(BlamLib.IO.EndianReader s);

		public virtual void Write(BlamLib.IO.EndianWriter s)
		{
			GroupTag1.Write(s);
			GroupTag2.Write(s);
			GroupTag3.Write(s);
		}
		#endregion
	};


	public abstract class CacheFileGen2 : Blam.CacheFile
	{
	};


	class CacheFileLanguagePackResourceGen2 : CacheFileLanguagePackResource
	{
		public const int kSizeOf = (4 * 2) + (4 * 4) + (1 + 3);

		public Blam.ResourcePtr GetOffsetReferences() { return OffsetReferences.Value; }
		public Blam.ResourcePtr GetOffsetStrings() { return OffsetStrings.Value; }

		/// <summary>
		/// Initialize the language pack to a tag definition or as a stand-alone
		/// </summary>
		/// <param name="parent"></param>
		public CacheFileLanguagePackResourceGen2(TI.Definition parent)
		{
			if (parent != null)
			{
				parent.Add(new TI.Pad(4 + 4)); // runtime pointers
					// s_cache_unicode_string_reference* references_buffer
					// byte* strings_buffer
				parent.Add(Count);
				parent.Add(Size);
				parent.Add(OffsetReferences);
				parent.Add(OffsetStrings);
				parent.Add(new TI.Pad(1 + 3));
			}
		}

		#region IStreamable Members
		public override void Read(BlamLib.IO.EndianReader s)
		{
			s.Seek(4 + 4, System.IO.SeekOrigin.Current);
			Count.Read(s);
			Size.Read(s);
			OffsetReferences.Read(s);
			OffsetStrings.Read(s);
			s.Seek(1 + 3, System.IO.SeekOrigin.Current);
		}

		public override void Write(BlamLib.IO.EndianWriter s)
		{
			s.Write(ulong.MinValue);
			Count.Write(s);
			Size.Write(s);
			OffsetReferences.Write(s);
			OffsetStrings.Write(s);
			s.Write(uint.MinValue);
		}
		#endregion

		#region Cache interop
		public override void ReadFromCache(Blam.CacheFile cf)
		{
			int count = this.Count.Value;

			if (count > 0)
			{
				var cache = cf as Halo2.CacheFile;

				#region Read the string references
				var rsrc_offset = GetOffsetReferences();
				var rsrc_cache = Program.Halo2.FromLocation(cache, rsrc_offset);

				if (rsrc_cache != null)
				{
					rsrc_cache.InputStream.Seek(rsrc_offset.Offset);
					InitializeStringReferences(count, cache.StringIds.Definition);
					for (int x = 0; x < mStringReferences.Length; x++)
						mStringReferences[x].Read(cf.InputStream);
				}
				#endregion

				#region Read the string data buffer
				rsrc_offset = GetOffsetStrings();
				rsrc_cache = Program.Halo2.FromLocation(cache, rsrc_offset);

				if (rsrc_cache != null)
				{
					rsrc_cache.InputStream.Seek(rsrc_offset.Offset);
					mStringData = rsrc_cache.InputStream.ReadBytes(Size.Value);
				}
				else // just in case references was valid but somehow the string data buffer wasn't, can't have one without the other
					mStringReferences = null;
				#endregion
			}
		}
		#endregion
	};
};