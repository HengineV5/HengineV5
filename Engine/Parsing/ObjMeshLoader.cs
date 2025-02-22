using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Engine.Graphics;

namespace Engine.Parsing
{
	internal class ObjMeshLoader
	{
		public static void Parse(StreamReader reader, ref Mesh mesh)
		{
			string name = null;

			List<Vector3f> positions = new();
			List<Vector3f> normals = new();
			List<Vector2f> texcoords = new();

			List<(uint, uint, uint)> triangles = new(11000);
			Dictionary<(uint, uint, uint), uint> vertexDictionary = new();

			List<uint> indicies = new();
			List<Vertex> verticies = new();

			string[] line = reader.ReadLine()?.Split(' ');
			while (line?.Length > 0)
			{
				switch (line[0])
				{
					case "o":
						name = ParseName(line);
						break;
					case "v":
						positions.Add(ParseVector3f(line));
						break;
					case "vt":
						texcoords.Add(ParseVector2f(line) * -1);
						break;
					case "vn":
						normals.Add(ParseVector3f(line));
						break;
					case "f":
						triangles.AddRange(ParseFaces(line));
						break;
					default:
						break;
				}

				line = reader.ReadLine()?.Split(' ');
			}

			foreach (var index in triangles)
			{
				if (!vertexDictionary.ContainsKey(index))
				{
					vertexDictionary.Add(index, (uint)verticies.Count);
					verticies.Add(new Vertex
					{
						position = positions[(int)index.Item1],
						normal = normals[(int)index.Item3],
						textureCoordinate = texcoords[(int)index.Item2]
					});
				}

				indicies.Add(vertexDictionary[index]);
			}

			//TODO: Fix
			if (verticies.Count > mesh.verticies.Length || indicies.Count > mesh.indicies.Length)
				throw new Exception("Mesh not big enough for model");

			for (int i = 0; i < verticies.Count; i++)
			{
				mesh.verticies[i] = verticies[i];
			}

			for (int i = 0; i < indicies.Count; i++)
			{
				mesh.indicies[i] = indicies[i];
			}
		}

		static string ParseName(string[] line)
		{
			return line[1];
		}

		static Vector3f ParseVector3f(string[] line)
		{
			return new Vector3f(float.Parse(line[1]), float.Parse(line[2]), float.Parse(line[3]));
		}

		static Vector2f ParseVector2f(string[] line)
		{
			return new Vector2f(float.Parse(line[1]), float.Parse(line[2]));
		}

		static IEnumerable<(uint, uint, uint)> ParseFaces(string[] line)
		{
			List<(uint, uint, uint)> indicies = new List<(uint, uint, uint)>();
			for (int i = 1; i < line.Length; i++)
			{
				if (line[i].Length == 0)
					continue;

				uint i1 = uint.Parse(line[i].Split('/')[0]);
				uint i2 = uint.Parse(line[i].Split('/')[1]);
				uint i3 = uint.Parse(line[i].Split('/')[2]);

				indicies.Add((i1 - 1, i2 - 1, i3 - 1));
			}

			return indicies;
		}
	}
}
