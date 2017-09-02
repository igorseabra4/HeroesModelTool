using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

static class Module1
{
    public enum InputFileType
    {
        OBJ
    }

    public struct Vertex
    {
        public float PositionX;
        public float PositionY;
        public float PositionZ;

        public byte ColorRed;
        public byte ColorGreen;
        public byte ColorBlue;
        public byte ColorAlpha;

        public float PositionU;
        public float PositionV;

        public bool HasUV;
    }

    public struct UVCoord
    {
        public float PositionU;
        public float PositionV;
    }

    public struct Triangle
    {
        public int MaterialIndex;

        public int Vertex1;
        public int Vertex2;
        public int Vertex3;

        public int UVCoord1;
        public int UVCoord2;
        public int UVCoord3;
    }
    
    static List<string> MaterialStream = new List<string>();
    static List<Vertex> VertexStream = new List<Vertex>();
    static List<UVCoord> UVStream = new List<UVCoord>();
    static List<Triangle> TriangleStream = new List<Triangle>();
    static string MTLLib;

    public static void Main()
    {
        System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

        string[] Arguments = Environment.GetCommandLineArgs();

        Console.WriteLine("============================================");
        Console.WriteLine("| Heroes Model Tool release 1 by igorseabra4");
        Console.WriteLine("| Usage: drag .OBJ model files into the executable to convert them to Sonic Heroes .BSP.");
        Console.WriteLine("| Just opening the program will convert every file found in the folder.");
        Console.WriteLine("| Dragging Sonic Heroes .BSP into the program will convert those to .OBJ (you have to drag those).");
        Console.WriteLine("============================================");

        if (Arguments.Length > 1)
        {
            foreach (string i in Arguments)
                if (i.Substring(i.Length - 3, 3).ToLower().Equals("obj".ToLower()))
                    ConvertOBJtoBSP(i);
                else if (i.Substring(i.Length - 3, 3).ToLower().Equals("bsp".ToLower()))
                    ConvertBSPtoOBJ(i);
        }
        else
        {
            string[] FilesInFolder = Directory.GetFiles(Directory.GetCurrentDirectory());

            foreach (string i in FilesInFolder)
                if (i.Substring(i.Length - 3, 3).ToLower().Equals("obj".ToLower()))
                    ConvertOBJtoBSP(i);
        }
        Console.ReadKey();
    }

    public static void ConvertOBJtoBSP(string FileName)
    {
        MaterialStream.Clear();
        VertexStream.Clear();
        UVStream.Clear();
        TriangleStream.Clear();
        MTLLib = null;

        Console.WriteLine("Reading " + FileName);

        ReadOBJFile(FileName);
        CreateBSPFile(FileName, InputFileType.OBJ);

        Console.WriteLine("Success.");
    }
    
    public static void ReadOBJFile(string InputFile)
    {
        string[] OBJFile = File.ReadAllLines(InputFile);

        int CurrentMaterial = -1;

        foreach (string j in OBJFile)
        {
            if (j.Length > 2)
            {
                if (j.Substring(0, 2) == "v ")
                {
                    if (j.Substring(2, 1) == " ")
                    {
                        string[] SubStrings = j.Split(' ');
                        Vertex TempVertex = new Vertex();
                        TempVertex.PositionX = Convert.ToSingle(SubStrings[2]);
                        TempVertex.PositionY = Convert.ToSingle(SubStrings[3]);
                        TempVertex.PositionZ = Convert.ToSingle(SubStrings[4]);

                        VertexStream.Add(TempVertex);
                    }
                    else
                    {
                        string[] SubStrings = j.Split(' ');
                        Vertex TempVertex = new Vertex();
                        TempVertex.PositionX = Convert.ToSingle(SubStrings[1]);
                        TempVertex.PositionY = Convert.ToSingle(SubStrings[2]);
                        TempVertex.PositionZ = Convert.ToSingle(SubStrings[3]);

                        VertexStream.Add(TempVertex);
                    }

                }
                else if (j.Substring(0, 3) == "vt ")
                {
                    string[] SubStrings = j.Split(' ');
                    UVCoord TempUV = new UVCoord();
                    TempUV.PositionU = Convert.ToSingle(SubStrings[1]);
                    TempUV.PositionV = Convert.ToSingle(SubStrings[2]);
                    UVStream.Add(TempUV);

                }
                else if (j.StartsWith("f "))
                {
                    string[] SubStrings = j.Split(' ');
                    Triangle TempTriangle = new Triangle();

                    TempTriangle.MaterialIndex = CurrentMaterial;

                    TempTriangle.Vertex1 = Convert.ToInt32(SubStrings[1].Split('/')[0]) - 1;
                    TempTriangle.UVCoord1 = Convert.ToInt32(SubStrings[1].Split('/')[1]) - 1;
                    TempTriangle.Vertex2 = Convert.ToInt32(SubStrings[2].Split('/')[0]) - 1;
                    TempTriangle.UVCoord2 = Convert.ToInt32(SubStrings[2].Split('/')[1]) - 1;
                    TempTriangle.Vertex3 = Convert.ToInt32(SubStrings[3].Split('/')[0]) - 1;
                    TempTriangle.UVCoord3 = Convert.ToInt32(SubStrings[3].Split('/')[1]) - 1;

                    TriangleStream.Add(TempTriangle);
                }
                else if (j.Length > 7)
                        if (j.Substring(0, 7) == "usemtl ")
                        {
                            MaterialStream.Add(j.Substring(7));
                            CurrentMaterial += 1;
                        }
                        else if (j.Substring(0, 7) == "mtllib ")
                            MTLLib = j.Substring(7);
            }
        }

        try { ReplaceMaterialNames(); }
        catch { Console.WriteLine("Unable to load material lib. Will use material names as texture names."); }

        FixUVCoords();
    }

    public static void ReplaceMaterialNames()
    {
        string[] MTLFile = File.ReadAllLines(MTLLib);

        string MaterialName = "";
        string TextureName = "";

        foreach (string j in MTLFile)
        {
            if (j.StartsWith("newmtl ") | j.StartsWith('\t' + "newmtl "))
            {
                MaterialName = j.Substring(7);
            }

            if (j.StartsWith("map_Kd ") | j.StartsWith('\t' + "map_Kd "))
            {
                TextureName = j.Substring(7).Split('\\').Last();  
                for (int k = 0; k < MaterialStream.Count; k++)
                {
                    if (MaterialStream[k] == MaterialName)
                    {
                        MaterialStream[k] = TextureName.Substring(0, TextureName.Length - 4);
                    }
                }
            }
        }
    }

    public static void FixUVCoords()
    {
        for (int i = 0; i < TriangleStream.Count; i++)
        {
            if (VertexStream[TriangleStream[i].Vertex1].HasUV == false)
            {
                Vertex TempVertex = VertexStream[TriangleStream[i].Vertex1];

                TempVertex.PositionU = UVStream[TriangleStream[i].UVCoord1].PositionU;
                TempVertex.PositionV = UVStream[TriangleStream[i].UVCoord1].PositionV;
                TempVertex.HasUV = true;
                VertexStream[TriangleStream[i].Vertex1] = TempVertex;
            }
            else
            {
                Vertex TempVertex = VertexStream[TriangleStream[i].Vertex1];

                if ((TempVertex.PositionU != UVStream[TriangleStream[i].UVCoord1].PositionU) | (TempVertex.PositionV != UVStream[TriangleStream[i].UVCoord1].PositionV))
                {
                    TempVertex.PositionU = UVStream[TriangleStream[i].UVCoord1].PositionU;
                    TempVertex.PositionV = UVStream[TriangleStream[i].UVCoord1].PositionV;

                    Triangle TempTriangle = TriangleStream[i];
                    TempTriangle.Vertex1 = VertexStream.Count;
                    TriangleStream[i] = TempTriangle;
                    VertexStream.Add(TempVertex);
                }
            }
            if (VertexStream[TriangleStream[i].Vertex2].HasUV == false)
            {
                Vertex TempVertex = VertexStream[TriangleStream[i].Vertex2];

                TempVertex.PositionU = UVStream[TriangleStream[i].UVCoord2].PositionU;
                TempVertex.PositionV = UVStream[TriangleStream[i].UVCoord2].PositionV;
                TempVertex.HasUV = true;
                VertexStream[TriangleStream[i].Vertex2] = TempVertex;
            }
            else
            {
                Vertex TempVertex = VertexStream[TriangleStream[i].Vertex2];

                if ((TempVertex.PositionU != UVStream[TriangleStream[i].UVCoord2].PositionU) | (TempVertex.PositionV != UVStream[TriangleStream[i].UVCoord2].PositionV))
                {
                    TempVertex.PositionU = UVStream[TriangleStream[i].UVCoord2].PositionU;
                    TempVertex.PositionV = UVStream[TriangleStream[i].UVCoord2].PositionV;

                    Triangle TempTriangle = TriangleStream[i];
                    TempTriangle.Vertex2 = VertexStream.Count;
                    TriangleStream[i] = TempTriangle;
                    VertexStream.Add(TempVertex);
                }
            }
            if (VertexStream[TriangleStream[i].Vertex3].HasUV == false)
            {
                Vertex TempVertex = VertexStream[TriangleStream[i].Vertex3];

                TempVertex.PositionU = UVStream[TriangleStream[i].UVCoord3].PositionU;
                TempVertex.PositionV = UVStream[TriangleStream[i].UVCoord3].PositionV;
                TempVertex.HasUV = true;
                VertexStream[TriangleStream[i].Vertex3] = TempVertex;
            }
            else
            {
                Vertex TempVertex = VertexStream[TriangleStream[i].Vertex3];

                if ((TempVertex.PositionU != UVStream[TriangleStream[i].UVCoord3].PositionU) | (TempVertex.PositionV != UVStream[TriangleStream[i].UVCoord3].PositionV))
                {
                    TempVertex.PositionU = UVStream[TriangleStream[i].UVCoord3].PositionU;
                    TempVertex.PositionV = UVStream[TriangleStream[i].UVCoord3].PositionV;

                    Triangle TempTriangle = TriangleStream[i];
                    TempTriangle.Vertex3 = VertexStream.Count;
                    TriangleStream[i] = TempTriangle;
                    VertexStream.Add(TempVertex);
                }
            }
        }
    }

    public static void CreateBSPFile(string OutputFile, InputFileType FileType)
    {
        BinaryWriter BSPWriter = new BinaryWriter(new FileStream(Path.ChangeExtension(OutputFile, "BSP"), FileMode.Create));
        Console.WriteLine("Creating " + Path.ChangeExtension(OutputFile, "BSP"));

        const UInt32 RenderWare = 0x1400ffff;

        long SavePosition = 0;

        float MaxX = VertexStream[0].PositionX;
        float MaxY = VertexStream[0].PositionY;
        float MaxZ = VertexStream[0].PositionZ;
        float MinX = VertexStream[0].PositionX;
        float MinY = VertexStream[0].PositionY;
        float MinZ = VertexStream[0].PositionZ;

        foreach (Vertex i in VertexStream)
        {
            if (i.PositionX > MaxX)
                MaxX = i.PositionX;
            if (i.PositionY > MaxY)
                MaxY = i.PositionY;
            if (i.PositionZ > MaxZ)
                MaxZ = i.PositionZ;
            if (i.PositionX < MinX)
                MinX = i.PositionX;
            if (i.PositionY < MinY)
                MinY = i.PositionY;
            if (i.PositionZ < MinZ)
                MinZ = i.PositionZ;
        }

        //// WORLD SECTION
        BSPWriter.Write(0xb);
        //Int32 0x0B // section identifier
        long WorldSectionSizeLocation = BSPWriter.BaseStream.Position;
        BSPWriter.Write(0);
        //Int32 // section size
        BSPWriter.Write(RenderWare);
        //Int32 0x1400FFFF

        //// MODEL HEADER STRUCT
        BSPWriter.Write(0x1);
        //Int32 0x01 // section identifier
        BSPWriter.Write(0x40);
        //Int32 // section size
        BSPWriter.Write(RenderWare);
        //Int32 0x1400FFFF
        BSPWriter.Write(0x1);
        //Int32 0x01 // unknown, always this value
        BSPWriter.Write(new byte[] {0, 0, 0, 0x80});
        //00 00 00 80 // unknown, always this value
        BSPWriter.Write(new byte[] { 0, 0, 0, 0x80 });
        //00 00 00 80 // unknown, always this value
        BSPWriter.Write(new byte[] { 0, 0, 0, 0x80 });
        //00 00 00 80 // unknown, always this value
        BSPWriter.Write(TriangleStream.Count);
        //Int32 // Number of triangles (numTriangles)
        BSPWriter.Write(VertexStream.Count);
        //Int32 // Number of vertices (numVertices)
        BSPWriter.Write(0);
        //00 00 00 00 // unknown, always this value
        BSPWriter.Write(1);
        //01 00 00 00 // unknown, always this value
        BSPWriter.Write(0);
        //00 00 00 00 // unknown, always this value
        BSPWriter.Write(new byte[] { 0xD, 0, 1, 0x40 });
        //0D 00 01 40 // unknown, always this value
        BSPWriter.Write(MaxX);
        BSPWriter.Write(MaxY);
        BSPWriter.Write(MaxZ);
        //float32[3] // Boundary box maximum
        BSPWriter.Write(MinX);
        BSPWriter.Write(MinY);
        BSPWriter.Write(MinZ);
        //float32[3] // Boundary box minimum // Maximum values must be the bigger than minimum

        //No need for size here
        //// END MODEL HEADER STRUCT

        //// MATERIAL LIST SECTION
        BSPWriter.Write(0x8);
        //Int32 0x08 // section identifier
        long MaterialListSizeLocation = BSPWriter.BaseStream.Position;
        BSPWriter.Write(0);
        //Int32 // section size
        BSPWriter.Write(RenderWare);
        //Int32 0x1400FFFF

        //// MATERIAL NUMBER STRUCT
        BSPWriter.Write(0x1);
        //Int32 0x01 // section identifier
        long MaterialNumberStructSizeLocation = BSPWriter.BaseStream.Position;
        BSPWriter.Write(0);
        //Int32 // section size
        BSPWriter.Write(RenderWare);
        //Int32 0x1400FFFF
        BSPWriter.Write(MaterialStream.Count);
        //Int32 // Number of materials (numMaterials), materials are ordered by a zero-based index
        foreach (string i in MaterialStream)
            BSPWriter.Write(new byte[] { 0xff, 0xff, 0xff, 0xff });
        //numMaterials* Int32 0xFFFFFFFF // there Is a -1 for each material

        long MaterialNumberStructSize = BSPWriter.BaseStream.Position - MaterialNumberStructSizeLocation - 8;
        SavePosition = BSPWriter.BaseStream.Position;
        BSPWriter.BaseStream.Position = MaterialNumberStructSizeLocation;
        BSPWriter.Write(Convert.ToUInt32(MaterialNumberStructSize));
        BSPWriter.BaseStream.Position = SavePosition;
        //// END MATERIAL NUMBER STRUCT

        foreach (string i in MaterialStream)
        {
            //// MATERIAL REF SECTION // this section occours numMaterials times
            BSPWriter.Write(0x7);
            //Int32 0x07 // section identifier
            long MaterialRefSectionSizeLocation = BSPWriter.BaseStream.Position;
            BSPWriter.Write(0);
            //Int32 // section size
            BSPWriter.Write(RenderWare);
            //Int32 0x1400FFFF

            //// MATERIAL STRUCT
            BSPWriter.Write(0x1);
            //Int32 0x01 // section identifier
            BSPWriter.Write(0x1c);
            //Int32 // section size (always 0x1C)
            BSPWriter.Write(RenderWare);
            //Int32 0x1400FFFF
            BSPWriter.Write(0);
            //Int32 0x00 // ununsed flags
            BSPWriter.Write(new byte[] { 0xff, 0xff, 0xff, 0xff });
            //int8[4] RGBA // Material RGBA (usually 255, 255, 255, 255)
            BSPWriter.Write(new byte[] { 0x84, 0x3e, 0xf5, 0x2d });
            //Int32 84 3E F5 2D // always this value, unused
            BSPWriter.Write(1);
            //bool32 // uses texture? (usually 0x01)
            BSPWriter.Write(Convert.ToSingle(1));
            BSPWriter.Write(Convert.ToSingle(1));
            BSPWriter.Write(Convert.ToSingle(1));
            //float32[3] // ambient, specular, diffuse // don't know if these are used. always (1, 1, 1)

            //No need for size here
            //// END MATERIAL STRUCT

            //// TEXTURE SECTION
            BSPWriter.Write(0x6);
            //Int32 0x06 // section identifier
            long TextureSectionSizeLocation = BSPWriter.BaseStream.Position;
            BSPWriter.Write(0);
            //Int32 // section size
            BSPWriter.Write(RenderWare);
            //Int32 0x1400FFFF

            //// TEXTURE FLAG STRUCT
            BSPWriter.Write(1);
            //Int32 0x01 // section identifier
            BSPWriter.Write(4);
            //Int32 // section size (always 0x04)
            BSPWriter.Write(RenderWare);
            //Int32 0x1400FFFF
            BSPWriter.Write(Convert.ToByte(2));
            //int8 // Byte, texture filtering mode (usually 0x02)
            BSPWriter.Write(Convert.ToByte(0x11));
            //4bit // half the byte: U adressing mode (usually 0001)
            //4bit // half the byte: V adressing mode (usually 0001)
            BSPWriter.Write(Convert.ToUInt16(0x1));
            //bool16 // Byte: use mipmap?(usually 0x01)

            //No need for size here
            //// END TEXTURE FLAG STRUCT

            //// DIFFUSE TEXTURE NAME SECTION
            BSPWriter.Write(2);
            //Int32 0x02 // section identifier
            int DiffuseTextureNameSize = i.Length;
            if (i.Length % 4 == 1)
                DiffuseTextureNameSize += 3;
            else if (i.Length % 4 == 2)
                DiffuseTextureNameSize += 2;
            else if (i.Length % 4 == 3)
                DiffuseTextureNameSize += 1;
            else if (i.Length % 4 == 0)
                DiffuseTextureNameSize += 4;
            BSPWriter.Write(DiffuseTextureNameSize);
            //Int32 // section size
            BSPWriter.Write(new byte[] { 0xff, 0xff, 0x0 });
            //Int32 0x1400FFFF
            long ByteBeforeTextureNameStarts = BSPWriter.BaseStream.Position;
            BSPWriter.Write(i);
            //String // texture name

            if (i.Length % 4 == 0)
            {
                BSPWriter.Write(Convert.ToByte(0));
                BSPWriter.Write(Convert.ToByte(0));
                BSPWriter.Write(Convert.ToByte(0));
                BSPWriter.Write(Convert.ToByte(0));
            }
            else if (i.Length % 4 == 1)
            {
                BSPWriter.Write(Convert.ToByte(0));
                BSPWriter.Write(Convert.ToByte(0));
                BSPWriter.Write(Convert.ToByte(0));
            }
            else if (i.Length % 4 == 2)
            {
                BSPWriter.Write(Convert.ToByte(0));
                BSPWriter.Write(Convert.ToByte(0));
            }
            else if (i.Length % 4 == 3)
            {
                BSPWriter.Write(Convert.ToByte(0));
            }

            SavePosition = BSPWriter.BaseStream.Position;
            BSPWriter.BaseStream.Position = ByteBeforeTextureNameStarts;
            BSPWriter.Write(Convert.ToByte(0x14));
            BSPWriter.BaseStream.Position = SavePosition;

            //No need for size here
            //// END DIFFUSE TEXTURE NAME SECTION

            //// ALPHA TEXTURE NAME SECTION // unused section, alphas are set in the TXD
            BSPWriter.Write(2);
            //Int32 0x02 // section identifier
            BSPWriter.Write(4);
            //Int32 // section size (always 0x04)
            BSPWriter.Write(RenderWare);
            //Int32 0x1400FFFF
            BSPWriter.Write(0);
            //String // alpha texture name (unused, always left blank)

            //No need for size here
            //// END ALPHA TEXTURE NAME SECTION

            //// TEXTURE EXTENSION // this section does absolutely nothing
            BSPWriter.Write(3);
            //Int32 0x03 // section identifier
            BSPWriter.Write(0);
            //Int32 // section size (0x00)
            BSPWriter.Write(RenderWare);
            //Int32 0x1400FFFF

            //No need for size here
            //// END TEXTURE EXTENSION

            long TextureSectionSize = BSPWriter.BaseStream.Position - TextureSectionSizeLocation - 8;
            SavePosition = BSPWriter.BaseStream.Position;
            BSPWriter.BaseStream.Position = TextureSectionSizeLocation;
            BSPWriter.Write(Convert.ToUInt32(TextureSectionSize));
            BSPWriter.BaseStream.Position = SavePosition;
            //// END TEXTURE SECTION

            //// MATERIAL EXTENSION // this section does absolutely nothing
            BSPWriter.Write(3);
            //Int32 0x03 // section identifier
            BSPWriter.Write(0);
            //Int32 // section size (0x00)
            BSPWriter.Write(RenderWare);
            //Int32 0x1400FFFF

            //No need for size here
            //// END MATERIAL EXTENSION

            long MaterialRefSectionSize = BSPWriter.BaseStream.Position - MaterialRefSectionSizeLocation - 8;
            SavePosition = BSPWriter.BaseStream.Position;
            BSPWriter.BaseStream.Position = MaterialRefSectionSizeLocation;
            BSPWriter.Write(Convert.ToUInt32(MaterialRefSectionSize));
            BSPWriter.BaseStream.Position = SavePosition;
            //// END MATERIAL REF SECTION
        }

        long MaterialListSize = BSPWriter.BaseStream.Position - MaterialListSizeLocation - 8;
        SavePosition = BSPWriter.BaseStream.Position;
        BSPWriter.BaseStream.Position = MaterialListSizeLocation;
        BSPWriter.Write(Convert.ToUInt32(MaterialListSize));
        BSPWriter.BaseStream.Position = SavePosition;
        //// END MATERIAL LIST SECTION

        //// ATOMIC SECTION
        BSPWriter.Write(9);
        //Int32 0x09 // section identifier
        long AtomicSectionSizeLocation = BSPWriter.BaseStream.Position;
        BSPWriter.Write(0);
        //Int32 // section size
        BSPWriter.Write(RenderWare);
        //Int32 0x1400FFFF

        //// ATOMIC STRUCT
        BSPWriter.Write(1);
        //Int32 0x01 // section identifier
        long AtomicStructSizeLocation = BSPWriter.BaseStream.Position;
        BSPWriter.Write(0);
        //Int32 // section size
        BSPWriter.Write(RenderWare);
        //Int32 0x1400FFFF
        BSPWriter.Write(0);
        //Int32 // model flags (usually 0x00)
        BSPWriter.Write(TriangleStream.Count);
        //Int32 // Number of triangles (numTriangles)
        BSPWriter.Write(VertexStream.Count);
        //Int32 // Number of vertices (numVertices)
        BSPWriter.Write(MaxX);
        BSPWriter.Write(MaxY);
        BSPWriter.Write(MaxZ);
        //float32[3] // Boundary box maximum
        BSPWriter.Write(MinX);
        BSPWriter.Write(MinY);
        BSPWriter.Write(MinZ);
        //float32[3] // Boundary box minimum // These last 6 values are the same ones in the model header
        BSPWriter.Write(new byte[] { 0x84, 0xd9, 0x50, 0x2f });
        //84 D9 50 2F // always this, unknown
        BSPWriter.Write(0);
        //Int32 0x00 // unknown, always 0x00

        foreach (Vertex v in VertexStream)
        {
            BSPWriter.Write(v.PositionX);
            BSPWriter.Write(v.PositionY);
            BSPWriter.Write(v.PositionZ);
        }
        //numVertices* float32[3] // X, Y, Z position coordinate for each vertex

        if (FileType == InputFileType.OBJ)
            for (int i = 0; i < VertexStream.Count; i++)
                BSPWriter.Write(new byte[] { 255, 255, 255, 255 });

        //numVertices* int8[4] // RGBA vertex color for each vertex

        foreach (Vertex v in VertexStream)
        {
            BSPWriter.Write(v.PositionU);
			BSPWriter.Write(-(v.PositionV));
		}
		//numVertices* float32[2] // U, V texture mapping coordinate for each vertex

		foreach (Triangle f in TriangleStream) {
			BSPWriter.Write(Convert.ToUInt16(f.MaterialIndex));
			BSPWriter.Write(Convert.ToUInt16(f.Vertex1));
			BSPWriter.Write(Convert.ToUInt16(f.Vertex2));
			BSPWriter.Write(Convert.ToUInt16(f.Vertex3));
		}
		//numTriangles* Int16[4] // (materialIndex, vertex1, vertex2, vertex3) index for each triangle, culling Is counterclockwise

		long AtomicStructSize = BSPWriter.BaseStream.Position - AtomicStructSizeLocation - 8;
        SavePosition = BSPWriter.BaseStream.Position;
		BSPWriter.BaseStream.Position = AtomicStructSizeLocation;
		BSPWriter.Write(Convert.ToUInt32(AtomicStructSize));
		BSPWriter.BaseStream.Position = SavePosition;
		//// END ATOMIC STRUCT

		//// ATOMIC EXTENSION
		BSPWriter.Write(3);
		//Int32 0x03 // section identifier
		long AtomicExtensionSizeLocation = BSPWriter.BaseStream.Position;
        BSPWriter.Write(0);
		//Int32 // section size
		BSPWriter.Write(RenderWare);
		//Int32 0x1400FFFF

		//// BIN MESH PLG SECTION
		BSPWriter.Write(0x50e);
        //Int32 0x50E // section identifier
        long BinMeshPLGSizeLocation = BSPWriter.BaseStream.Position;
        BSPWriter.Write(0);
		//Int32 // section size
		BSPWriter.Write(RenderWare);
		//Int32 0x1400FFFF
		BSPWriter.Write(0);
		//UInt32 // flags(0 = Triangle lists, 1 = Triangle strips; Sonic Heroes always uses tristrips, don't know if it even supports trilists)
		BSPWriter.Write(MaterialStream.Count);
        //        UInt32 // Number of objects/meshes (numMeshes; usually same number of materials)
        long TotalNumberOfTristripIndiciesLocation = BSPWriter.BaseStream.Position;
        long TotalNumberOfTristripIndicies = 0;
        BSPWriter.Write(0);
		//UInt32 // total number of indices

		for (int i = 0; i < MaterialStream.Count; i++) {
			List<Triangle> TriangleStream2 = new List<Triangle>();

			foreach (Triangle f in TriangleStream) {
				if (f.MaterialIndex == i) {
					TriangleStream2.Add(f);
				}
			}

			long NumberOfTristripIndiciesLocation = BSPWriter.BaseStream.Position;
            BSPWriter.Write(0);
			//    UInt32 // Number of vertex indices in this mesh (numIndices)
			BSPWriter.Write(i);
			//    UInt32 // material index

			//    UInt32[numIndices] // Vertex indices
			foreach (Triangle t in TriangleStream2) {
				BSPWriter.Write(t.Vertex1);
				BSPWriter.Write(t.Vertex2);
				BSPWriter.Write(t.Vertex3);
			}

			long NumberOfTristripIndicies = (BSPWriter.BaseStream.Position - NumberOfTristripIndiciesLocation - 8) / 4;
            SavePosition = BSPWriter.BaseStream.Position;
			BSPWriter.BaseStream.Position = NumberOfTristripIndiciesLocation;
			BSPWriter.Write(Convert.ToUInt32(NumberOfTristripIndicies));
			BSPWriter.BaseStream.Position = SavePosition;

			TotalNumberOfTristripIndicies += NumberOfTristripIndicies;
		}

		SavePosition = BSPWriter.BaseStream.Position;
		BSPWriter.BaseStream.Position = TotalNumberOfTristripIndiciesLocation;
		BSPWriter.Write(Convert.ToUInt32(TotalNumberOfTristripIndicies));
		BSPWriter.BaseStream.Position = SavePosition;

		long BinMeshPLGSize = BSPWriter.BaseStream.Position - BinMeshPLGSizeLocation - 8;
        SavePosition = BSPWriter.BaseStream.Position;
		BSPWriter.BaseStream.Position = BinMeshPLGSizeLocation;
		BSPWriter.Write(Convert.ToUInt32(BinMeshPLGSize));
		BSPWriter.BaseStream.Position = SavePosition;
		//// END BIN MESH PLG SECTION

		long AtomicExtensionSize = BSPWriter.BaseStream.Position - AtomicExtensionSizeLocation - 8;
        SavePosition = BSPWriter.BaseStream.Position;
		BSPWriter.BaseStream.Position = AtomicExtensionSizeLocation;
		BSPWriter.Write(Convert.ToUInt32(AtomicExtensionSize));
		BSPWriter.BaseStream.Position = SavePosition;
        //// END ATOMIC EXTENSION

        long AtomicSectionSize = BSPWriter.BaseStream.Position - AtomicSectionSizeLocation - 8;
        SavePosition = BSPWriter.BaseStream.Position;
		BSPWriter.BaseStream.Position = AtomicSectionSizeLocation;
		BSPWriter.Write(Convert.ToUInt32(AtomicSectionSize));
		BSPWriter.BaseStream.Position = SavePosition;
		//// END ATOMIC SECTION

		//// WORLD EXTENSION // this section does absolutely nothing
		BSPWriter.Write(3);
		//Int32 0x03 // section identifier
		BSPWriter.Write(0);
		//Int32 // section size (0x00)
		BSPWriter.Write(RenderWare);
        //Int32 0x1400FFFF

        //// END WORLD EXTENSION

        long WorldSectionSize = BSPWriter.BaseStream.Position - WorldSectionSizeLocation - 8;
        SavePosition = BSPWriter.BaseStream.Position;
		BSPWriter.BaseStream.Position = WorldSectionSizeLocation;
		BSPWriter.Write(Convert.ToUInt32(WorldSectionSize));
		BSPWriter.BaseStream.Position = SavePosition;
		//// END WORLD SECTION
	}     

    public static void ConvertBSPtoOBJ(string FileName)
    {
        MaterialStream.Clear();
        VertexStream.Clear();
        TriangleStream.Clear();

        Console.WriteLine("Reading " + FileName);
        try
        {
            if (ReadBSPFile(FileName))
            {
                FixBSPMaterialNames();
                CreateOBJFile(FileName);
                Console.WriteLine("Success.");
            }
        }
        catch
        {
            Console.WriteLine("Error.");
        }
    }

    public static bool ReadBSPFile(string InputFileName)
    {
        BinaryReader BSPReader = new BinaryReader(new FileStream(InputFileName, FileMode.Open));

        BSPReader.BaseStream.Position = 0x4;
        if (BSPReader.ReadUInt32() + 0xc != BSPReader.BaseStream.Length)
            throw NotABSPFile();

        BSPReader.BaseStream.Position = 0x28;
        UInt32 NumTriangles = BSPReader.ReadUInt32();
        UInt32 NumVertices = BSPReader.ReadUInt32();
        BSPReader.BaseStream.Position = 0x70;
        UInt32 NumMaterials = BSPReader.ReadUInt32();

        BSPReader.BaseStream.Position += 4 * NumMaterials;

        for (int i = 0; i < NumMaterials; i++)
        {
            BSPReader.BaseStream.Position += 0x54;
            int TextureSize = BSPReader.ReadInt32();
            BSPReader.BaseStream.Position += 0x4;
            char[] TextureName = BSPReader.ReadChars(TextureSize);
            BSPReader.BaseStream.Position += 0x28;

            MaterialStream.Add(new string(TextureName));
        }

        BSPReader.BaseStream.Position += 0x1c;

        if (BSPReader.ReadUInt32() != NumTriangles)
            throw NotABSPFile();

        if (BSPReader.ReadUInt32() != NumVertices)
            throw NotABSPFile();

        BSPReader.BaseStream.Position += 0x20;

        for (int i = 0; i < NumVertices; i++)
        {
            Vertex TempVertex = new Vertex();
            TempVertex.PositionX = BSPReader.ReadSingle();
            TempVertex.PositionY = BSPReader.ReadSingle();
            TempVertex.PositionZ = BSPReader.ReadSingle();

            VertexStream.Add(TempVertex);
        }

        for (int i = 0; i < NumVertices; i++)
        {
            Vertex TempVertex = VertexStream[i];
            TempVertex.ColorRed = BSPReader.ReadByte();
            TempVertex.ColorGreen = BSPReader.ReadByte();
            TempVertex.ColorBlue = BSPReader.ReadByte();
            TempVertex.ColorAlpha = BSPReader.ReadByte();

            VertexStream[i] = TempVertex;
        }

        byte[] TempArray = new byte[4];

        for (int i = 0; i < NumVertices; i++)
        {
            Vertex TempVertex = VertexStream[i];

            TempArray = BSPReader.ReadBytes(4);
            TempVertex.PositionU = (BitConverter.ToSingle(TempArray, 0));
            TempArray = BSPReader.ReadBytes(4);
            TempVertex.PositionV = 1 - (BitConverter.ToSingle(TempArray, 0));

            VertexStream[i] = TempVertex;
        }

        for (int i = 0; i < NumTriangles; i++)
        {
            Triangle TempTriangle = new Triangle();

            TempTriangle.MaterialIndex = BSPReader.ReadUInt16();
            TempTriangle.Vertex1 = BSPReader.ReadUInt16();
            TempTriangle.Vertex2 = BSPReader.ReadUInt16();
            TempTriangle.Vertex3 = BSPReader.ReadUInt16();

            TriangleStream.Add(TempTriangle);
        }
        return true;
    }

    private static Exception NotABSPFile()
    {
        throw new NotImplementedException();
    }

    public static void FixBSPMaterialNames()
    {
        for (int i = 0; i < MaterialStream.Count; i++)
            MaterialStream[i] = MaterialStream[i].Split('\x0').First();
    }

    public static void CreateOBJFile(string OutputFileName)
    {
        string MaterialLibrary = Path.ChangeExtension(OutputFileName, "MTL");

        StreamWriter OBJWriter = new StreamWriter((Path.ChangeExtension(OutputFileName, "OBJ")), false);

        string FileName = OutputFileName.Split('\\').Last().ToLower();
        FileName = FileName.Substring(0, FileName.Length - 4);

        OBJWriter.WriteLine("#Exported by HeroesModelTool");
        OBJWriter.WriteLine("#Number of vertices: " + VertexStream.Count.ToString());
        OBJWriter.WriteLine("#Number of faces: " + TriangleStream.Count.ToString());
        OBJWriter.WriteLine();
        OBJWriter.WriteLine("mtllib " + MaterialLibrary.Split('\\').Last());
        OBJWriter.WriteLine();

        //Write vertex list to obj
        foreach (Vertex i in VertexStream)
            OBJWriter.WriteLine("v " + i.PositionX.ToString() + " " + i.PositionY.ToString() + " " + i.PositionZ.ToString());

        OBJWriter.WriteLine();

        //Write uv list to obj
        foreach (Vertex i in VertexStream)
            OBJWriter.WriteLine("vt " + i.PositionU.ToString() + " " + i.PositionV.ToString());

        OBJWriter.WriteLine();

        for (int i = 0; i < MaterialStream.Count; i++)
        {
            OBJWriter.WriteLine();
            OBJWriter.WriteLine("g " + FileName + "_" + i.ToString());
            OBJWriter.WriteLine("usemtl " + MaterialStream[i] + "_m");

            foreach (Triangle j in TriangleStream)
                if (j.MaterialIndex == i)
                    OBJWriter.WriteLine("f " + (j.Vertex1 + 1).ToString() + "/" + (j.Vertex1 + 1).ToString() + " " + (j.Vertex2 + 1).ToString() + "/" + (j.Vertex2 + 1).ToString() + " " + (j.Vertex3 + 1).ToString() + "/" + (j.Vertex3 + 1).ToString());
        }
        OBJWriter.WriteLine();

        OBJWriter.Close();

        StreamWriter MTLWriter = new StreamWriter(MaterialLibrary, false);
        MTLWriter.WriteLine("#Exported by HeroesModelTool");

        for (int i = 0; i < MaterialStream.Count; i++)
        {
            MTLWriter.WriteLine("newmtl " + MaterialStream[i] + "_m");
            MTLWriter.WriteLine('\t' + "map_Kd " + MaterialStream[i] + ".png");
        }

        MTLWriter.Close();
    }
}