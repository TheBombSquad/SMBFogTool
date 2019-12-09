using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SMBFogTool
{
    public class Program
    {
        public enum fogTypes
        {
            GX_FOG_NONE = 0,
            GX_FOG_LIN = 2,
            GX_FOG_EXP = 4,
            GX_FOG_EXP2 = 5,
            GX_FOG_REVEXP = 6,
            GX_FOG_REVEXP2 = 7
        }

        public enum easingTypes
        {
            CONSTANT, LINEAR, EASED
        }
        [Serializable]
        [System.Xml.Serialization.XmlRootAttribute("fogHeader")]
        public struct fogHeader
        {
            public fogTypes fogType;
            public Single fogStartDistance;
            public Single fogEndDistance;
            public Vector3 color;
        }
        [Serializable]
        [System.Xml.Serialization.XmlRootAttribute("fogAnimation")]
        public struct fogAnimation
        {
            public List<keyframe> startDistanceKeyframes;
            public List<keyframe> endDistanceKeyframes;
            public List<keyframe> redKeyframes;
            public List<keyframe> greenKeyframes;
            public List<keyframe> blueKeyframes;
            public List<keyframe> unknownKeyframes;

            public fogAnimation(List<keyframe> startDistanceKeyframes, List<keyframe> endDistanceKeyframes, List<keyframe> redKeyframes, List<keyframe> greenKeyframes, List<keyframe> blueKeyframes, List<keyframe> unknownKeyframes)
            {
                this.startDistanceKeyframes = startDistanceKeyframes;
                this.endDistanceKeyframes = endDistanceKeyframes;
                this.redKeyframes = redKeyframes;
                this.greenKeyframes = greenKeyframes;
                this.blueKeyframes = blueKeyframes;
                this.unknownKeyframes = unknownKeyframes;
            }
        }

        public struct keyframe
        {
            public easingTypes easingType;
            public Single time;
            public Single value;
            public Single easingValue1;
            public Single easingValue2;
        }

        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("\n\tSMB Fog Tool\n");
                Console.WriteLine("\tThis is a tool for importing and exporting fog and fog animaton headers to and from SMB2 stagedef files.\n");
                Console.WriteLine("\tUsage:");
                Console.WriteLine("\t  SMBFogTool -i [source]\t\t\tExtracts fog data from input stagdef to an XML file.");
                Console.WriteLine("\t  SMBFogTool -i [source] -o [destination]\tCopies fog data from source to the destination stagedef.");
                Console.WriteLine("\t  SMBFogTool -i [source] -c [XML file names*]\tCopies the fog data from XML files.");
                Console.WriteLine("\t\t* Use ONLY the shared file name. For example, for 'test.fog.xml' and 'test.foganim.xml', use 'test'.\n");
                Console.WriteLine("\tAt least one keyframe with identical settings to the header is required for fog to show up in-game.");
                Console.WriteLine("\tThere are 6 types of fog defined in SMB2: ");
                Console.WriteLine("\t\tGX_FOG_NONE, GX_FOG_LIN, GX_FOG_EXP, GX_FOG_EXP2, GX_FOG_REVEXP, GX_FOG_REVEXP2");
                Console.WriteLine("\tColor is stored as (x, y, z), where x, y, and z represent red, green and blue.");
                Console.WriteLine("\tEach value of color is stored as a real number from 0 to 1, where 1 is the maximum value.");
                Console.WriteLine("\tTo convert a typical 0-255 RGB color value, simply divide the value by 255.");

                return;
            }
            try
            {

                if (args.Length == 2) // input stagedef, output fog data file
                {
                    using var inputReader = new BinaryReaderBigEndian(File.Open(args[1], FileMode.Open));
                    fogHeader header = extractFogData(inputReader);
                    fogAnimation animation = extractFogAnimationData(inputReader);
                    inputReader.Close();

                    exportFog(header, animation, true, args[1]);
                }

                else if (args.Length == 4)
                {
                    if (args[2].ToString().Equals("-c"))
                    { // input fog data file and stagedef, output new stagedef
                        System.Xml.Serialization.XmlSerializer headerSerializer = new System.Xml.Serialization.XmlSerializer(typeof(fogHeader), new XmlRootAttribute("fogHeader"));
                        System.Xml.Serialization.XmlSerializer animationSerializer = new System.Xml.Serialization.XmlSerializer(typeof(fogAnimation), new XmlRootAttribute("fogAnimation"));
                        try
                        {
                            System.IO.FileStream fogDataFile = System.IO.File.Open(args[3] + ".fog.xml", FileMode.Open);
                            System.IO.FileStream fogAnimationFile = System.IO.File.Open(args[3] + ".foganim.xml", FileMode.Open);
                            fogHeader header = (fogHeader)headerSerializer.Deserialize(fogDataFile);
                            fogAnimation animation = (fogAnimation)animationSerializer.Deserialize(fogAnimationFile);

                            exportFog(header, animation, false, args[1]);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error occured opening XML files.");
                            return;
                        }
                    }

                    else if (args[2].ToString().Equals("-o"))
                    { // input source stagedef, output to destination stagedef
                        using var inputStageDefReader = new BinaryReaderBigEndian(File.Open(args[1], FileMode.Open));

                        fogHeader header = extractFogData(inputStageDefReader);
                        fogAnimation animation = extractFogAnimationData(inputStageDefReader);
                        inputStageDefReader.Close();

                        exportFog(header, animation, false, args[3]);
                    }
                }

                else
                {
                    Console.WriteLine("Invalid arguments.");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error occured:\n" + ex);
                return;
            }
        }

        public static fogHeader extractFogData(BinaryReaderBigEndian input)
        {
            fogHeader newHeader;
            input.BaseStream.Position = 0xBC;   // fog header offset location
            input.BaseStream.Position = input.ReadUInt32(); // fog header offset

            if (input.BaseStream.Position == 0)
            {
                throw new Exception("No fog header found!");
            }

            newHeader.fogType = (fogTypes)input.ReadByte();
            input.ReadBytes(3);     // unknown/null
            newHeader.fogStartDistance = input.ReadSingle();
            newHeader.fogEndDistance = input.ReadSingle();
            newHeader.color.X = input.ReadSingle();
            newHeader.color.Y = input.ReadSingle();
            newHeader.color.Z = input.ReadSingle();

            return newHeader;
        }

        public static fogAnimation extractFogAnimationData(BinaryReaderBigEndian input)
        {
            fogAnimation newAnimation = new fogAnimation();
            input.BaseStream.Position = 0xB0;   // fog animation header offset location
            uint currentHeaderOffset = input.ReadUInt32(); // fog animation header

            if (input.BaseStream.Position == 0)
            {
                throw new Exception("No fog animation header found!");
            }

            object newAnim = newAnimation;
            foreach (var field in typeof(fogAnimation).GetFields())
            {
                input.BaseStream.Position = currentHeaderOffset;
                uint keyframeCount = input.ReadUInt32();
                uint currentOffset = input.ReadUInt32();
                List<keyframe> currentList = new List<keyframe>();
                for (uint offsetIterate = currentOffset; offsetIterate < currentOffset + (keyframeCount * 0x14); offsetIterate += 0x14)
                {
                    keyframe currentKeyframe;
                    input.BaseStream.Position = offsetIterate;
                    currentKeyframe.easingType = (easingTypes)input.ReadUInt32();
                    currentKeyframe.time = input.ReadSingle();
                    currentKeyframe.value = input.ReadSingle();
                    currentKeyframe.easingValue1 = input.ReadSingle();
                    currentKeyframe.easingValue2 = input.ReadSingle();
                    currentList.Add(currentKeyframe);
                }
                field.SetValue(newAnim, currentList);
                currentHeaderOffset += 0x8;
            }
            newAnimation = (fogAnimation)newAnim;
            return newAnimation;
        }

        public static void exportFog(fogHeader newHeader, fogAnimation newAnimation, bool toXML, string outputStageDefPath)
        {

            if (toXML) //export to xml file
            {
                System.Xml.Serialization.XmlSerializer headerSerializer = new System.Xml.Serialization.XmlSerializer(typeof(fogHeader), new XmlRootAttribute("fogHeader"));
                System.Xml.Serialization.XmlSerializer animationSerializer = new System.Xml.Serialization.XmlSerializer(typeof(fogAnimation), new XmlRootAttribute("fogAnimation"));
                System.IO.FileStream fogDataFile = System.IO.File.Create(outputStageDefPath + ".fog.xml");
                System.IO.FileStream fogAnimationFile = System.IO.File.Create(outputStageDefPath + ".foganim.xml");
                XmlSerializerNamespaces empty = new XmlSerializerNamespaces();
                empty.Add("","");

                headerSerializer.Serialize(fogDataFile, newHeader, empty);
                animationSerializer.Serialize(fogAnimationFile, newAnimation, empty);
                fogDataFile.Close();
                fogAnimationFile.Close();
            }

            else //export to stagedef
            {
                long fileSize = new System.IO.FileInfo(outputStageDefPath).Length;
                File.Copy(outputStageDefPath, outputStageDefPath + ".out", true);
                using var outputStageDefWriter = new BinaryWriterBigEndian(File.Open(outputStageDefPath + ".out", FileMode.Open));
                outputStageDefWriter.BaseStream.Position = fileSize;
                while ((fileSize % 0x10) != 0) // writes padding to keep stagedef aligned
                {
                    outputStageDefWriter.Write((byte)0x0);
                    fileSize++;
                }
                // writes header
                uint newFogHeaderOffset = (uint)outputStageDefWriter.BaseStream.Position;

                outputStageDefWriter.Write((byte)newHeader.fogType);
                outputStageDefWriter.BaseStream.Position += 3;
                outputStageDefWriter.Write((Single)newHeader.fogStartDistance);
                outputStageDefWriter.Write((Single)newHeader.fogEndDistance);
                outputStageDefWriter.Write((Single)newHeader.color.X);
                outputStageDefWriter.Write((Single)newHeader.color.Y);
                outputStageDefWriter.Write((Single)newHeader.color.Z);

                // writes animation header

                uint newFogAnimationHeaderOffset = (uint)outputStageDefWriter.BaseStream.Position;
                uint newFogAnimationOffset = (uint) (newFogAnimationHeaderOffset + 0x30);
                uint currentHeaderOffset = (uint)newFogAnimationHeaderOffset;
                uint currentKeyframeOffset = (uint)newFogAnimationOffset;

                foreach (var field in typeof(fogAnimation).GetFields())
                {
                    outputStageDefWriter.BaseStream.Position = currentHeaderOffset;

                    List<keyframe> currentList = (List<keyframe>)field.GetValue(newAnimation);
                    outputStageDefWriter.Write((UInt32)currentList.Count);
                    outputStageDefWriter.Write((UInt32)currentKeyframeOffset);

                    outputStageDefWriter.BaseStream.Position = currentKeyframeOffset;
                    foreach (keyframe key in currentList)
                    {
                            outputStageDefWriter.Write(BitConverter.GetBytes((UInt32)key.easingType));
                            outputStageDefWriter.Write(BitConverter.GetBytes((Single)key.time));
                            outputStageDefWriter.Write(BitConverter.GetBytes((Single)key.value));
                            outputStageDefWriter.Write(BitConverter.GetBytes((Single)key.easingValue1));
                            outputStageDefWriter.Write(BitConverter.GetBytes((Single)key.easingValue2));
                    }
                    currentHeaderOffset += 8;
                    currentKeyframeOffset += (uint)(currentList.Count * 0x14);
                }

                // writes new offsets

                outputStageDefWriter.BaseStream.Position = 0xB0;
                outputStageDefWriter.Write(newFogAnimationHeaderOffset);
                outputStageDefWriter.BaseStream.Position = 0xBC;
                outputStageDefWriter.Write(newFogHeaderOffset);
            }
        }

    }
}
