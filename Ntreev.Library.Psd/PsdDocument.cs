﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Ntreev.Library.Psd
{
    public sealed class PsdDocument : IPsdLayer
    {
        private string name;
        private ColorModeData colorModeData;
        private DisplayInfo displayInfo;
        private FileHeader fileHeader;
        private GridAndGuidesInfo gridAndGuidesInfo;
        private PsdLayer[] layers;
        private ResolutionInfo resolutionInfo;
        private Channel[] channels;
        private LinkedLayer[] linkedLayers;
        private SliceInfo[] slices = new SliceInfo[] { };
        private GlobalLayerMask globalLayerMask;
        private Properties props = new Properties();

        public PsdDocument()
            : this("Root")
        {
            
        }

        public PsdDocument(string name)
        {
            this.name = name;
        }

        public void Read(string filename)
        {
            using (FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                this.Read(stream);
            }
        }

        public void Read(Stream stream)
        {
            using (PSDReader reader = new PSDReader(stream))
            {
                this.ReadFileHeader(reader);
                this.ReadColorModeData(reader);
                this.ReadImageResources(reader);
                this.ReadLayers(reader);
                this.ReadImageData(reader);
            }
        }

        public FileHeader FileHeader
        {
            get { return this.fileHeader; }
        }

        public ColorModeData ColorModeData
        {
            get { return this.colorModeData; }
        }

        public DisplayInfo DisplayInfo
        {
            get { return this.displayInfo; }
        }

        public ResolutionInfo ResolutionInfo
        {
            get { return this.resolutionInfo; }
        }

        public GridInfo GridInfo
        {
            get { return this.gridAndGuidesInfo.GridInfo; }
        }

        public GuidesInfo GuidesInfo
        {
            get { return this.gridAndGuidesInfo.GuidesInfo; }
        }

        public SliceInfo[] Slices
        {
            get { return this.slices; }
        }

        public int Width
        {
            get { return this.fileHeader.Width; }
        }

        public int Height
        {
            get { return this.fileHeader.Height; }
        }

        public int Depth
        {
            get { return this.fileHeader.Depth; }
        }

        public Channel[] Channels
        {
            get { return this.channels; }
        }

        public GlobalLayerMask GlobalLayerMask
        {
            get { return this.globalLayerMask; }
        }

        private void ReadColorModeData(PSDReader reader)
        {
            this.colorModeData = new ColorModeData(reader);
        }

        private void ReadFileHeader(PSDReader reader)
        {
            this.fileHeader = new FileHeader(reader);
            reader.Version = this.fileHeader.Version;
            if (this.fileHeader.Depth != 8)
            {
                throw new SystemException("For now, only Support 8 Bit Per Channel");
            }
        }

        private void ReadImageResources(PSDReader reader)
        {
            int size = reader.ReadInt32();
            long position = reader.Position;
            while ((reader.Position - position) < size)
            {
                string signature = reader.ReadAscii(4);
                if (signature != "8BIM")
                {
                    continue;
                }
                short imageResourceID = reader.ReadInt16();
                string name = reader.ReadPascalString(2);
                int resourceSize = reader.ReadInt32();
                if (resourceSize > 0)
                {
                    switch (imageResourceID)
                    {
                        case 0x0408:
                            this.gridAndGuidesInfo = new GridAndGuidesInfo(reader);
                            break;
                        case 0x3ed:
                            this.resolutionInfo = new ResolutionInfo(reader);
                            break;
                        case 0x3ef:
                            this.displayInfo = new DisplayInfo(reader);
                            break;
                        case 0x041a:
                            {
                                long ppp = reader.Position;
                                var version = reader.ReadInt32();
                                var r1 = reader.ReadInt32();
                                var r2 = reader.ReadInt32();
                                var r3 = reader.ReadInt32();
                                var r4 = reader.ReadInt32();
                                string text = reader.ReadUnicodeString();
                                var count = reader.ReadInt32();

                                List<SliceInfo> slices = new List<SliceInfo>(count);
                                for (int i = 0; i < count; i++)
                                {
                                    slices.Add(new SliceInfo(reader));
                                }

                                this.slices = slices.ToArray();

                                int descVer = reader.ReadInt32();
                                this.props.Add(string.Format("0x{0:x4}", imageResourceID), new DescriptorStructure(reader));
                            }
                            break;

                        default:
                            {
                                reader.Position += resourceSize;
                                break;
                            }
                    }
                    if ((resourceSize % 2) != 0)
                    {
                        reader.Position += 1L;
                    }
                }
            }
        }

        private void ReadLayers(PSDReader reader)
        {
            long length = reader.ReadLength();
            long end = reader.Position + length;

            this.layers = LayerInfo.ReadLayers(reader, this, this.fileHeader.Depth);
            LayerInfo.ComputeBounds(this.layers);
            this.globalLayerMask = new GlobalLayerMask(reader);

            List<string> keys = new List<string>(new string[]{"LMsk", "Lr16", "Lr32", "Layr", "Mt16", "Mt32", "Mtrn", "Alph", "FMsk", "lnk2", "FEid", "FXid", "PxSD",});
            List<LinkedLayer> linkedLayers = new List<LinkedLayer>();

            List<string> kkk = new List<string>();

            while (reader.Position < end)
            {
                string signature = reader.ReadAscii(4);
                string key = reader.ReadAscii(4);
                if (signature != "8BIM" && signature != "8B64")
                    throw new Exception();
                kkk.Add(key);

                long ssss = reader.Position;

                long l = 0;
                long p;

                if (keys.Contains(key) == true && reader.Version == 2)
                {
                    l = reader.ReadInt64();
                    p = reader.Position;
                }
                else
                {
                    l = reader.ReadInt32();
                    p = reader.Position;
                }

                switch (key)
                {
                    case "lnkD":
                    case "lnk2":
                    case "lnk3":
                        {
                            long e = reader.Position + l;
                            while (reader.Position < e)
                            {
                                //long p1 = reader.Position;
                                linkedLayers.Add(new LinkedLayer(reader));
                                //long l2 = (reader.Position - p1) % 4;
                                //reader.ReadBytes((int)l2);
                            }
                        }
                        break;
                    case "Patt":
                        {

                        }
                        break;
                    case "Txt2":
                        {
                            //reader.Position = p + l;

                            //byte b = reader.ReadByte();
                            //byte b2 = reader.ReadByte();
                            //byte b3 = reader.ReadByte();
                            //l += (l % 4);
                            //if (l % 4 == 2)
                            //    l += 2;
                        }
                        break;
                }

                reader.Position = p + l;
                if (reader.Position % 2 != 0)
                    reader.Position++;

                reader.Position += ((reader.Position - p) % 4);

                //reader.Position = p + l;
                ////if (l % 4 == 2)
                ////{
                ////    reader.Position = l % 4;
                ////}
                ////else 
                //    if(reader.Position % 2 != 0)
                //{
                //    reader.Position++;
                //}
            }
            this.linkedLayers = linkedLayers.ToArray();

            this.SetLinkedLayer(this.layers);
        }

        private void SetLinkedLayer(IEnumerable<PsdLayer> layers)
        {
            foreach (var item in layers)
            {
                if (item.PlacedID != Guid.Empty)
                {
                    item.LinkedLayer = this.linkedLayers.Where(i => i.ID == item.PlacedID && i.PSD != null).FirstOrDefault();
                }

                this.SetLinkedLayer(item.Childs);
            }
        }

        private void ReadImageData(PSDReader reader)
        {
            CompressionType compressionType = (CompressionType)reader.ReadInt16();

            ChannelType[] types = new ChannelType[] { ChannelType.Red, ChannelType.Green, ChannelType.Blue, ChannelType.Alpha, };
            Channel[] channels = new Channel[this.fileHeader.NumberOfChannels];

            for(int i=0; i <channels.Length ; i++)
            {
                channels[i] = new Channel(types[i], this.Width, this.Height);
            }

            for (int i = 0; i < channels.Length; i++)
            {
                channels[i] = new Channel(types[i], this.Width, this.Height);
                channels[i].LoadHeader(reader, compressionType);
            }

            for (int i = 0; i < channels.Length; i++)
            {
                channels[i].Load(reader, this.fileHeader.Depth, compressionType);
            }

            this.channels = channels.OrderBy(item => item.Type).ToArray();
        }

        public BlendMode BlendMode
        {
            get { return BlendMode.Normal; }
        }

        public IEnumerable<IPsdLayer> Childs
        {
            get { return this.layers; }
        }

        public string Name
        {
            get { return this.name; }
        }

        public float Opacity
        {
            get { return 1.0f; }
        }

        public IProperties Properties
        {
            get { return this.props; }
        }

        #region IPsdLayer

        IPsdLayer IPsdLayer.Parent
        {
            get { return null; }
        }

        bool IPsdLayer.IsClipping
        {
            get { return false; }
        }

        PsdDocument IPsdLayer.Document
        {
            get { return this; }
        }

        IPsdLayer IPsdLayer.LinkedLayer
        {
            get { return null; }
        }

        int IPsdLayer.Left
        {
            get { return 0; }
        }

        int IPsdLayer.Top
        {
            get { return 0; }
        }

        int IPsdLayer.Right
        {
            get { return this.Width; }
        }

        int IPsdLayer.Bottom
        {
            get { return this.Height; }
        }

        bool IImageSource.HasImage
        {
            get { return true; }
        }

        #endregion
    }
}

