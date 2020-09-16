﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Lidgren.Core;

namespace UnitTests
{
	[TestClass]
	public class DataWriterTests
	{
		[TestMethod]
		public void TestDataWriter()
		{
			using (var wrt = new DataWriter(3))
			{
				wrt.WriteBool(true);
				wrt.WriteDouble(1.88);
				wrt.WriteVariable(12345);
				wrt.WriteString("Pararibulitis");
				wrt.WriteVariable(0ul);

				var rdr = wrt.ReadOnlySpan;

				Assert.IsTrue(rdr.ReadBool());
				Assert.AreEqual(1.88, rdr.ReadDouble());
				Assert.AreEqual(12345, rdr.ReadVariableInt64());
				Assert.AreEqual("Pararibulitis", rdr.ReadString());
				Assert.AreEqual(0ul, rdr.ReadVariableUInt64());
			}
		}

		[TestMethod]
		public void TestDataWriter2()
		{
			ulong ss = RandomSeed.GetUInt64();

			byte[] small = new byte[64];

			using (var wrt = new DataWriter(1))
			{
				for (int runs = 0; runs < 10000; runs++)
				{
					wrt.Clear();
					var originalSeed = PRNG.NextUInt64(ref ss);

					var seed = originalSeed;
					{
						int numOps = PRNG.Next(ref seed, 1, 15);
						for (int o = 0; o < numOps; o++)
						{
							switch (PRNG.Next(ref seed, 0, 6))
							{
								case 0:
									wrt.WriteBool(PRNG.NextBool(ref seed));
									break;
								case 1:
									wrt.WriteSingle((float)PRNG.Next(ref seed, 1, 100));
									break;
								case 2:
									wrt.WriteVariable(PRNG.Next(ref seed, -50, 50));
									break;
								case 3:
									wrt.WriteString(PRNG.NextUInt32(ref seed).ToString());
									break;
								case 4:
									wrt.WriteUInt16((ushort)PRNG.Next(ref seed, 0, ushort.MaxValue));
									break;
								case 5:
									var sss = small.AsSpan(0, PRNG.Next(ref seed, 1, 25));
									wrt.WriteBytes(sss);
									break;
							}
						}
					}

					// read
					var rdr = wrt.ReadOnlySpan;

					seed = originalSeed;
					{
						int numOps = PRNG.Next(ref seed, 1, 15);
						for (int o = 0; o < numOps; o++)
						{
							switch (PRNG.Next(ref seed, 0, 6))
							{
								case 0:
									{
										var facit = PRNG.NextBool(ref seed);
										Assert.AreEqual(facit, rdr.ReadBool());
									}
									break;
								case 1:
									{
										var facit = (float)PRNG.Next(ref seed, 1, 100);
										Assert.AreEqual(facit, rdr.ReadSingle());
									}
									break;
								case 2:
									{
										var facit = PRNG.Next(ref seed, -50, 50);
										Assert.AreEqual(facit, rdr.ReadVariableInt64());
									}
									break;
								case 3:
									{
										var facit = PRNG.NextUInt32(ref seed).ToString();
										Assert.AreEqual(facit, rdr.ReadString());
									}
									break;
								case 4:
									{
										var facit = (ushort)PRNG.Next(ref seed, 0, ushort.MaxValue);
										Assert.AreEqual(facit, rdr.ReadUInt16());
									}
									break;
								case 5:
									{
										var sss = small.AsSpan(0, PRNG.Next(ref seed, 1, 25));
										var aaa = rdr.ReadBytes(sss.Length);
										Assert.IsTrue(sss.SequenceEqual(aaa));
									}
									break;
							}
						}
					}
				}
			}
		}
	}
}
