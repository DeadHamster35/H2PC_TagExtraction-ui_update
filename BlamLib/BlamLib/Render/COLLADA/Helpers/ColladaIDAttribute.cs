﻿/*
	BlamLib: .NET SDK for the Blam Engine

	See license\BlamLib\BlamLib for specific license information
*/
using System;

namespace BlamLib.Render.COLLADA
{
	[AttributeUsage(AttributeTargets.Property)]
	public class ColladaIDAttribute : System.Attribute
	{
		readonly string formatString;

		public ColladaIDAttribute(string format)
		{
			formatString = format;
		}

		public string FormatID(string id)
		{
			return String.Format(formatString, id);
		}
	}
}