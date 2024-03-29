/*
	BlamLib: .NET SDK for the Blam Engine

	See license\BlamLib\BlamLib for specific license information
*/
using System;
using System.Collections.Generic;
using System.Text;

namespace BlamLib.Blam.HaloReach
{
	#region Header
	/// <summary>
	/// Halo Reach implementation of the <see cref="Blam.CacheHeader"/>
	/// </summary>
	public class CacheHeader : Cache.CacheHeaderGen3
	{
		const int kSizeOfBeta = 0x4000;
		const int kSizeOf = 0xA000;

		internal System.Runtime.InteropServices.ComTypes.FILETIME Filetime;

		public override void Read(IO.EndianReader s)
		{
			int k_local_sizeof = Blam.CacheFile.ValidateHeader(s, kSizeOfBeta, kSizeOf);

			s.Seek(4);
			version = s.ReadInt32();
			if (version != 11 && version != 12) throw new InvalidCacheFileException(s.FileName);
			fileLength = s.ReadInt32();
			s.ReadInt32();
			tagIndexAddress = s.ReadUInt32();
			memoryBufferOffset = s.ReadInt32();
			memoryBufferSize = s.ReadInt32();

			sourceFile = s.ReadAsciiString(256);
			build = s.ReadTagString();
			cacheType = (Blam.CacheType)s.ReadInt16();
			sharedType = (Cache.SharedType)s.ReadInt16();

			s.ReadBool();
			s.ReadBool(); // false if it belongs to a untracked build
			s.ReadBool();
			s.ReadByte(); // appears to be an ODST-only field

			s.ReadInt32(); s.ReadInt32();
			s.ReadInt32(); s.ReadInt32(); s.ReadInt32();

			#region string id table
			// 0x158
			stringIdsCount = s.ReadInt32();
			stringIdsBufferSize = s.ReadInt32();
			stringIdIndicesOffset = s.ReadInt32();
			stringIdsBufferOffset = s.ReadInt32();
			#endregion

			#region filetimes?
			// pretty sure this is a flags field
			// used to tell which of the following 64bit values
			// are used. Damn sure this are FILETIME structures, but
			// hex workshop doesn't like them so I can't be for sure...
			needsShared = s.ReadInt32() != 0; // just a little 'hack' if you will. if zero, the map is self reliant, so no worries
			Filetime.dwHighDateTime = s.ReadInt32();
			Filetime.dwLowDateTime = s.ReadInt32();
			if (s.ReadInt32() != 0) flags.Add(Halo3.CacheHeaderFlags.DependsOnMainMenu); s.ReadInt32();
			if (s.ReadInt32() != 0) flags.Add(Halo3.CacheHeaderFlags.DependsOnShared); s.ReadInt32();
			if (s.ReadInt32() != 0) flags.Add(Halo3.CacheHeaderFlags.DependsOnCampaign); s.ReadInt32();
			#endregion

			name = s.ReadTagString();
			s.ReadInt32();
			scenarioPath = s.ReadAsciiString(256);
			s.ReadInt32(); // minor version, normally not used

			#region tag paths
			tagNamesCount = s.ReadInt32();
			tagNamesBufferOffset = s.ReadInt32(); // cstring buffer
			tagNamesBufferSize = s.ReadInt32(); // cstring buffer total size in bytes
			tagNameIndicesOffset = s.ReadInt32();
			#endregion

			checksum = s.ReadUInt32(); // 0x2C4
			s.Seek(32, System.IO.SeekOrigin.Current); // these bytes are always the same

			baseAddress = s.ReadUInt32(); // expected base address
			xdkVersion = s.ReadInt32(); // xdk version

			#region memory partitions
			// 0x2F0

			// memory partitions
			memoryPartitions = new Partition[6];
			memoryPartitions[0].BaseAddress = s.ReadUInt32(); // cache resource buffer
			memoryPartitions[0].Size = s.ReadInt32();

			// readonly
			memoryPartitions[1].BaseAddress = s.ReadUInt32(); // cache gestalt resource buffer
			memoryPartitions[1].Size = s.ReadInt32();

			memoryPartitions[2].BaseAddress = s.ReadUInt32(); // global tags buffer (cache sound tags likes this memory space too)
			memoryPartitions[2].Size = s.ReadInt32();
			memoryPartitions[3].BaseAddress = s.ReadUInt32(); // shared tag blocks? (havok data likes this memory space too)
			memoryPartitions[3].Size = s.ReadInt32();
			memoryPartitions[4].BaseAddress = s.ReadUInt32(); // address
			memoryPartitions[4].Size = s.ReadInt32();

			// readonly
			memoryPartitions[5].BaseAddress = s.ReadUInt32(); // map tags buffer
			memoryPartitions[5].Size = s.ReadInt32();
			#endregion

			int count = s.ReadInt32();
			s.Seek(4 + 8, System.IO.SeekOrigin.Current); // these bytes are always the same
			// if there is a hash in the header, this is the ONLY
			// place where it can be
			s.Seek(20 /*SHA1*/ + 40 + 256 /*RSA*/, System.IO.SeekOrigin.Current); // ???

			// 0x46C
			cacheInterop.Read(s);
			cacheInterop.PostprocessForCacheRead(k_local_sizeof);

			s.Seek(16, System.IO.SeekOrigin.Current); // GUID?, these bytes are always the same. ODST is different from Halo 3

			#region blah 1
			// 0x4AC

			// campaign has a shit load of these
			// but shared doesn't nor mainmenu
			// I compared the sc110 french and english and both have the SAME counts and element data. So 
			// I don't think this is a hash or something. At least, if it is, it's not runtime relative so 
			// nothing we have to worry about

			s.ReadInt16(); // I've only seen this be two different values (besides zero).
			count = s.ReadInt16(); // I think the above specifies the size of the structure this count represents?
			s.ReadInt32(); // seems to always be zero
			CompressionGuid = new Guid(s.ReadBytes(16));

			s.Seek(count * 28, System.IO.SeekOrigin.Current); // seek past the elements
			// dword
			// long
			// buffer [0x14] (probably a sha1 hash)
			s.Seek((320 - count) * 28, System.IO.SeekOrigin.Current); // seek past the unused elements
			#endregion

			// this shit has changed. fuck you.
			#region blah 2
#if false
			{
				// 0x27C4
				// This on the other hand, sc110 french and english had MINOR differences. Those differences were in 
				// DWORDs @ 0x4 and 0x8

				// going to punt and just assume there is a max count of 10 of these possible

				// maybe related to bsp\'zones'?
				const int blah2_sizeof = 180;

				count = (int)(s.ReadUInt32() >> 24); // did someone forget to fucking byte swap something?
				s.Seek(count * blah2_sizeof, System.IO.SeekOrigin.Current); // seek past the elements
				s.Seek((10 - count) * blah2_sizeof, System.IO.SeekOrigin.Current); // seek past the unused elements
			}
#endif
			#endregion

			//s.Seek(300 + sizeof(uint), System.IO.SeekOrigin.Current); // zero
			s.Seek(6200 + sizeof(uint), System.IO.SeekOrigin.Current);


			ReadPostprocessForInterop();

			ReadPostprocessForBaseAddresses(s);
		}
	};
	#endregion

	#region Index
	/// <summary>
	/// Halo Reach engine base implementation of the <see cref="Blam.CacheIndex"/>
	/// </summary>
	public sealed class CacheIndex : Blam.Cache.CacheIndexGen3
	{
		public override void Read(BlamLib.IO.EndianReader s)
		{
			CacheFile cache = s.Owner as CacheFile;

			#region read body
			groupTagsCount = s.ReadInt32();
			groupTagsOffset = (groupTagsAddress = s.ReadUInt32()) - cache.AddressMask;

			tagCount = s.ReadInt32();
			tagsOffset = (address = s.ReadUInt32()) - cache.AddressMask;

			dependentTagsCount = s.ReadInt32();
			dependentTagsOffset = (dependentTagsAddress = s.ReadUInt32()) - cache.AddressMask;

			s.Seek(sizeof(int) +
				sizeof(uint) + // seems to always be the same address as the dependent tags
				sizeof(int), // new to Reach. Only seen as zero so far
				System.IO.SeekOrigin.Current);

			s.ReadUInt32(); // crc?
			s.ReadInt32(); // 'tags'
			#endregion

			ReadDependents(s);

			ReadTagInstances(s);

			ReadGroupTags(s);

			#region fixup group tag info
			for (int x = 0; x < tagCount; x++)
			{
				var item = items[x];
				if (!item.IsEmpty)
				{
					item.GroupTag = groupTags[item.Datum.Index].GroupTag1;
					item.GroupTagInt = item.GroupTag.ID;
					item.Fixup((ushort)x); // Fix the hacks Bungie did for Halo3
				}
			}
			#endregion

			#region Load tag names
			using (var buffer = cache.DecryptCacheSegmentStream(CacheSectionType.Tag, cache.HeaderHaloReach.TagNamesBufferOffset, cache.HeaderHaloReach.TagNamesBufferSize))
			{
				string tag_name;
				foreach (var ci in items)
				{
					if (!ci.IsEmpty)
					{
						// NOTE, we're recording a buffer relative offset here...since the 
						// cache has it encrypted I don't see the point of having the 
						// offset relative to the cache file
						ci.TagNameOffset = buffer.PositionUnsigned;
						tag_name = buffer.ReadCString();
						if (tag_name == "") tag_name = "NONE";
					}
					else
					{
						ci.TagNameOffset = uint.MaxValue;
						ci.ReferenceName = DatumIndex.Null;
						continue;
					}

					ci.ReferenceName = cache.References.AddOptimized(ci.GroupTag, tag_name);
				}
			}
			#endregion
		}

		void ReadGroupTags(BlamLib.IO.EndianReader s)
		{
			groupTags = new CacheItemGroupTag[groupTagsCount];
			s.Seek(groupTagsOffset, System.IO.SeekOrigin.Begin);
			for (int x = 0; x < groupTagsCount; x++)
				(groupTags[x] = new CacheItemGroupTag()).Read(s);
		}
	};
	#endregion

	#region ItemGroupTag
	public sealed class CacheItemGroupTag : Cache.CacheItemGroupTagGen3
	{
		#region IStreamable Members
		public override void Read(BlamLib.IO.EndianReader s)
		{
			base.ReadGroupTags(Program.HaloReach.Manager, s);

			Name.Read(s, (s.Owner as CacheFile).StringIds.Definition.Description);
		}
		#endregion
	};
	#endregion

	#region File
	/// <summary>
	/// Halo Reach engine base implementation of the <see cref="Blam.CacheFile"/>
	/// </summary>
	public sealed class CacheFile : Blam.Cache.CacheFileGen3
	{
		#region GetCacheFileResourceDefinitionFactory
		class CacheFileResourceDefinitionFactory : Cache.Tags.CacheFileResourceDefinitionFactory
		{
		};
		public override Cache.Tags.CacheFileResourceDefinitionFactory GetCacheFileResourceDefinitionFactory()
		{
			return new CacheFileResourceDefinitionFactory();
		}
		#endregion

		public override bool ResourcesUseEncryption { get { return true; } }

		internal override byte[] DecryptCacheSegmentBytes(CacheSectionType section_type, int segment_offset, int segment_size)
		{
			InputStream.Seek(segment_offset);
			uint buffer_size = Util.Align(16, (uint)segment_size);

			byte[] encrypted = InputStream.ReadBytes(buffer_size);
			byte[] decrypted;
			GameDefinition.SecurityAesDecrypt(engineVersion, section_type, encrypted, out decrypted);

			return decrypted != null ? decrypted : null;
		}

		#region Header
		HaloReach.CacheHeader cacheHeader = null;
		public override BlamLib.Blam.CacheHeader Header { get { return cacheHeader; } }

		public HaloReach.CacheHeader HeaderHaloReach { get { return cacheHeader; } }
		#endregion

		public override string GetUniqueName()
		{
			var ft = HeaderHaloReach.Filetime;
			return string.Format("{0}_{1}{2}", HeaderHaloReach.Name, ft.dwHighDateTime.ToString("X8"), ft.dwLowDateTime.ToString("X8"));
		}

		#region Index
		HaloReach.CacheIndex cacheIndex = null;
		public override BlamLib.Blam.CacheIndex Index { get { return cacheIndex; } }

		public HaloReach.CacheIndex IndexHaloReach { get { return cacheIndex; } }
		#endregion

		#region StringIdManager
		protected override IO.EndianReader GetStringIdsBuffer(ICacheHeaderStringId sid_header)
		{
			return DecryptCacheSegmentStream(CacheSectionType.Debug, sid_header.StringIdsBufferOffset, sid_header.StringIdsBufferSize);
		}
		#endregion

		bool SharableReferenceXbox(string path)
		{
			if (SharableReference(path, Program.HaloReach.XboxMainmenu)) ShareCacheStreams(this, Program.HaloReach.XboxMainmenu);
			else if (SharableReference(path, Program.HaloReach.XboxShared)) ShareCacheStreams(this, Program.HaloReach.XboxShared);
			else if (SharableReference(path, Program.HaloReach.XboxCampaign)) ShareCacheStreams(this, Program.HaloReach.XboxCampaign);
			else return false;

			return true;
		}
		bool SharableReferencePc(string path) { return false; }

		public CacheFile(string map_name)
		{
			if (!SharableReferenceXbox(map_name) && !SharableReferencePc(map_name))
			{
				InputStream = new IO.EndianReader(map_name, IO.EndianState.Big, this);
				if (!CacheIsReadonly(map_name))
					OutputStream = new IO.EndianWriter(map_name, IO.EndianState.Big, this);
			}

			cacheIndex = new CacheIndex();
			cacheHeader = new CacheHeader();
		}

		internal override void ReadResourceCache()
		{
			if (isLoaded) return;

			cacheHeader.Read(InputStream);

			isLoaded = true;
		}

		void DetermineEngineVersion()
		{
			var str = cacheHeader.Build.Substring(20);

			switch (str)
			{
				case "omaha_beta":
				case "omaha_delta":
					engineVersion = BlamVersion.HaloReach_Beta;
					break;

				case "omaha_relea":
					engineVersion = BlamVersion.HaloReach_Xbox;
					break;

				default:
					throw new Debug.Exceptions.UnreachableException(str);
			}
		}

		void TagIndexInitializeAndRead(BlamLib.IO.EndianReader s)
		{
			base.StringIdManagerInitializeAndRead();
			base.InitializeReferenceManager(s.FileName);
			base.InitializeTagIndexManager();

			s.Seek(cacheHeader.OffsetToIndex, System.IO.SeekOrigin.Begin);
			cacheIndex.Read(s);
		}

		public override void Read(BlamLib.IO.EndianReader s)
		{
			if (isLoaded) return;

			cacheHeader.Read(s);

			DetermineEngineVersion();

			// Shared caches can't be loaded from here
			if (cacheHeader.SharedType == Cache.SharedType.Shared || 
				cacheHeader.SharedType == Cache.SharedType.Campaign) 
				throw new SharedCacheAccessException();

			TagIndexInitializeAndRead(s);

			isLoaded = true;
		}
	};
	#endregion
}