/*  MapleLib - A general-purpose MapleStory library
 * Copyright (C) 2009, 2010, 2015 Snow and haha01haha01
   
 * This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

 * This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

 * You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MapleLib.WzLib.Util;
using MapleLib.WzLib.WzProperties;
using System.IO;
using System.Drawing.Imaging;
using System.Globalization;
using System.Xml;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace MapleLib.WzLib.Serialization
{
    public abstract class ProgressingWzSerializer
    {
        protected int total = 0;
        protected int curr = 0;
        public int Total { get { return total; } }
        public int Current { get { return curr; } }

        protected static void createDirSafe(ref string path)
        {
            if (path.Substring(path.Length - 1, 1) == @"\") path = path.Substring(0, path.Length - 1);
            string basePath = path;
            int curridx = 0;
            while (Directory.Exists(path) || File.Exists(path))
            {
                curridx++;
                path = basePath + curridx;
            }
            Directory.CreateDirectory(path);
        }
    }

    public abstract class WzJsonSerializer : ProgressingWzSerializer
    {
        static WzJsonSerializer() { }
        public WzJsonSerializer() { }
    }

    public abstract class WzXmlSerializer : ProgressingWzSerializer
    {
        protected string indent;
        protected string lineBreak;
        public static NumberFormatInfo formattingInfo;
        protected bool ExportBase64Data = false;

        protected static char[] amp = "&amp;".ToCharArray();
        protected static char[] lt = "&lt;".ToCharArray();
        protected static char[] gt = "&gt;".ToCharArray();
        protected static char[] apos = "&apos;".ToCharArray();
        protected static char[] quot = "&quot;".ToCharArray();

        static WzXmlSerializer()
        {
            formattingInfo = new NumberFormatInfo();
            formattingInfo.NumberDecimalSeparator = ".";
            formattingInfo.NumberGroupSeparator = ",";
        }

        public WzXmlSerializer(int indentation, LineBreak lineBreakType)
        {
            switch (lineBreakType)
            {
                case LineBreak.None:
                    lineBreak = "";
                    break;
                case LineBreak.Windows:
                    lineBreak = "\r\n";
                    break;
                case LineBreak.Unix:
                    lineBreak = "\n";
                    break;
            }
            char[] indentArray = new char[indentation];
            for (int i = 0; i < indentation; i++)
                indentArray[i] = (char)0x20;
            indent = new string(indentArray);
        }

        protected void WritePropertyToXML(TextWriter tw, string depth, WzImageProperty prop)
        {
            if (prop is WzCanvasProperty)
            {
                WzCanvasProperty property3 = (WzCanvasProperty)prop;
                if (ExportBase64Data)
                {
                    MemoryStream stream = new MemoryStream();
                    property3.PngProperty.GetPNG(false).Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                    byte[] pngbytes = stream.ToArray();
                    stream.Close();
                    tw.Write(string.Concat(new object[] { depth, "<canvas name=\"", XmlUtil.SanitizeText(property3.Name), "\" width=\"", property3.PngProperty.Width, "\" height=\"", property3.PngProperty.Height, "\" basedata=\"", Convert.ToBase64String(pngbytes), "\">" }) + lineBreak);
                }
                else
                    tw.Write(string.Concat(new object[] { depth, "<canvas name=\"", XmlUtil.SanitizeText(property3.Name), "\" width=\"", property3.PngProperty.Width, "\" height=\"", property3.PngProperty.Height, "\">" }) + lineBreak);
                string newDepth = depth + indent;
                foreach (WzImageProperty property in property3.WzProperties)
                    WritePropertyToXML(tw, newDepth, property);
                tw.Write(depth + "</canvas>" + lineBreak);
            }
            else if (prop is WzIntProperty)
            {
                WzIntProperty property4 = (WzIntProperty)prop;
                tw.Write(string.Concat(new object[] { depth, "<int name=\"", XmlUtil.SanitizeText(property4.Name), "\" value=\"", property4.Value, "\"/>" }) + lineBreak);
            }
            else if (prop is WzDoubleProperty)
            {
                WzDoubleProperty property5 = (WzDoubleProperty)prop;
                tw.Write(string.Concat(new object[] { depth, "<double name=\"", XmlUtil.SanitizeText(property5.Name), "\" value=\"", property5.Value, "\"/>" }) + lineBreak);
            }
            else if (prop is WzNullProperty)
            {
                WzNullProperty property6 = (WzNullProperty)prop;
                tw.Write(depth + "<null name=\"" + XmlUtil.SanitizeText(property6.Name) + "\"/>" + lineBreak);
            }
            else if (prop is WzSoundProperty)
            {
                WzSoundProperty property7 = (WzSoundProperty)prop;
                if (ExportBase64Data)
                    tw.Write(string.Concat(new object[] { depth, "<sound name=\"", XmlUtil.SanitizeText(property7.Name), "\" length=\"", property7.Length.ToString(), "\" basehead=\"", Convert.ToBase64String(property7.Header), "\" basedata=\"", Convert.ToBase64String(property7.GetBytes(false)), "\"/>" }) + lineBreak);
                else
                    tw.Write(depth + "<sound name=\"" + XmlUtil.SanitizeText(property7.Name) + "\"/>" + lineBreak);
            }
            else if (prop is WzStringProperty)
            {
                WzStringProperty property8 = (WzStringProperty)prop;
                string str = XmlUtil.SanitizeText(property8.Value);
                tw.Write(depth + "<string name=\"" + XmlUtil.SanitizeText(property8.Name) + "\" value=\"" + str + "\"/>" + lineBreak);
            }
            else if (prop is WzSubProperty)
            {
                WzSubProperty property9 = (WzSubProperty)prop;
                tw.Write(depth + "<imgdir name=\"" + XmlUtil.SanitizeText(property9.Name) + "\">" + lineBreak);
                string newDepth = depth + indent;
                foreach (WzImageProperty property in property9.WzProperties)
                    WritePropertyToXML(tw, newDepth, property);
                tw.Write(depth + "</imgdir>" + lineBreak);
            }
            else if (prop is WzShortProperty)
            {
                WzShortProperty property10 = (WzShortProperty)prop;
                tw.Write(string.Concat(new object[] { depth, "<short name=\"", XmlUtil.SanitizeText(property10.Name), "\" value=\"", property10.Value, "\"/>" }) + lineBreak);
            }
            else if (prop is WzLongProperty)
            {
                WzLongProperty long_prop = (WzLongProperty)prop;
                tw.Write(string.Concat(new object[] { depth, "<long name=\"", XmlUtil.SanitizeText(long_prop.Name), "\" value=\"", long_prop.Value, "\"/>" }) + lineBreak);
            }
            else if (prop is WzUOLProperty)
            {
                WzUOLProperty property11 = (WzUOLProperty)prop;
                tw.Write(depth + "<uol name=\"" + property11.Name + "\" value=\"" + XmlUtil.SanitizeText(property11.Value) + "\"/>" + lineBreak);
            }
            else if (prop is WzVectorProperty)
            {
                WzVectorProperty property12 = (WzVectorProperty)prop;
                tw.Write(string.Concat(new object[] { depth, "<vector name=\"", XmlUtil.SanitizeText(property12.Name), "\" x=\"", property12.X.Value, "\" y=\"", property12.Y.Value, "\"/>" }) + lineBreak);
            }
            else if (prop is WzFloatProperty)
            {
                WzFloatProperty property13 = (WzFloatProperty)prop;
                string str2 = Convert.ToString(property13.Value, formattingInfo);
                if (!str2.Contains("."))
                    str2 = str2 + ".0";
                tw.Write(depth + "<float name=\"" + XmlUtil.SanitizeText(property13.Name) + "\" value=\"" + str2 + "\"/>" + lineBreak);
            }
            else if (prop is WzConvexProperty)
            {
                tw.Write(depth + "<extended name=\"" + XmlUtil.SanitizeText(prop.Name) + "\">" + lineBreak);
                WzConvexProperty property14 = (WzConvexProperty)prop;
                string newDepth = depth + indent;
                foreach (WzImageProperty property in property14.WzProperties)
                    WritePropertyToXML(tw, newDepth, property);
                tw.Write(depth + "</extended>" + lineBreak);
            }
        }
    }

    public interface IWzFileSerializer
    {
        void SerializeFile(WzFile file, string path);
    }

    public interface IWzDirectorySerializer : IWzFileSerializer
    {
        void SerializeDirectory(WzDirectory dir, string path);
    }

    public interface IWzImageSerializer : IWzDirectorySerializer
    {
        void SerializeImage(WzImage img, string path);
    }

    public interface IWzObjectSerializer
    {
        void SerializeObject(WzObject file, string path);
    }

    public enum LineBreak
    {
        None,
        Windows,
        Unix
    }

    public class NoBase64DataException : System.Exception
    {
        public NoBase64DataException() : base() { }
        public NoBase64DataException(string message) : base(message) { }
        public NoBase64DataException(string message, System.Exception inner) : base(message, inner) { }
        protected NoBase64DataException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
        { }
    }

    public class WzImgSerializer : ProgressingWzSerializer, IWzImageSerializer
    {

        public byte[] SerializeImage(WzImage img)
        {
            total = 1; curr = 0;

            using (MemoryStream stream = new MemoryStream())
            {
                using (WzBinaryWriter wzWriter = new WzBinaryWriter(stream, ((WzDirectory)img.parent).WzIv))
                {
                    img.SaveImage(wzWriter);
                    byte[] result = stream.ToArray();

                    return result;
                }
            }
        }

        public void SerializeImage(WzImage img, string outPath)
        {
            total = 1; curr = 0;
            if (Path.GetExtension(outPath) != ".img")
            {
                outPath += ".img";
            }
            using (FileStream stream = File.Create(outPath))
            {
                using (WzBinaryWriter wzWriter = new WzBinaryWriter(stream, ((WzDirectory)img.parent).WzIv))
                {
                    img.SaveImage(wzWriter);
                }
            }
        }

        public void SerializeDirectory(WzDirectory dir, string outPath)
        {
            total = dir.CountImages();
            curr = 0;
            if (!Directory.Exists(outPath))
                WzXmlSerializer.createDirSafe(ref outPath);

            if (outPath.Substring(outPath.Length - 1, 1) != @"\")
            {
                outPath += @"\";
            }

            foreach (WzDirectory subdir in dir.WzDirectories)
            {
                SerializeDirectory(subdir, outPath + subdir.Name + @"\");
            }
            foreach (WzImage img in dir.WzImages)
            {
                SerializeImage(img, outPath + img.Name);
            }
        }

        public void SerializeFile(WzFile f, string outPath)
        {
            SerializeDirectory(f.WzDirectory, outPath);
        }
    }


    public class WzImgDeserializer : ProgressingWzSerializer
    {
        private bool freeResources;

        public WzImgDeserializer(bool freeResources)
            : base()
        {
            this.freeResources = freeResources;
        }

        public WzImage WzImageFromIMGBytes(byte[] bytes, WzMapleVersion version, string name, bool freeResources)
        {
            byte[] iv = WzTool.GetIvByMapleVersion(version);
            MemoryStream stream = new MemoryStream(bytes);
            WzBinaryReader wzReader = new WzBinaryReader(stream, iv);
            WzImage img = new WzImage(name, wzReader);
            img.BlockSize = bytes.Length;
            img.Checksum = 0;
            foreach (byte b in bytes) img.Checksum += b;
            img.Offset = 0;
            if (freeResources)
            {
                img.ParseImage(true);
                img.Changed = true;
                wzReader.Close();
            }
            return img;
        }

        /// <summary>
        /// Parse a WZ image from .img file/
        /// </summary>
        /// <param name="inPath"></param>
        /// <param name="iv"></param>
        /// <param name="name"></param>
        /// <param name="successfullyParsedImage"></param>
        /// <returns></returns>
        public WzImage WzImageFromIMGFile(string inPath, byte[] iv, string name, out bool successfullyParsedImage)
        {
            FileStream stream = File.OpenRead(inPath);
            WzBinaryReader wzReader = new WzBinaryReader(stream, iv);

            WzImage img = new WzImage(name, wzReader);
            img.BlockSize = (int)stream.Length;
            img.Checksum = 0;
            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes, 0, (int)stream.Length);
            stream.Position = 0;
            foreach (byte b in bytes) img.Checksum += b;
            img.Offset = 0;
            if (freeResources)
            {
                successfullyParsedImage = img.ParseImage(true);
                img.Changed = true;
                wzReader.Close();
            }
            else
            {
                successfullyParsedImage = true;
            }
            return img;
        }
    }


    public class WzPngMp3Serializer : ProgressingWzSerializer, IWzImageSerializer, IWzObjectSerializer
    {
        //List<WzImage> imagesToUnparse = new List<WzImage>();
        private string outPath;

        public void SerializeObject(WzObject obj, string outPath)
        {
            //imagesToUnparse.Clear();
            total = 0; curr = 0;
            this.outPath = outPath;
            if (!Directory.Exists(outPath)) WzXmlSerializer.createDirSafe(ref outPath);
            if (outPath.Substring(outPath.Length - 1, 1) != @"\") outPath += @"\";
            total = CalculateTotal(obj);
            ExportRecursion(obj, outPath);
            /*foreach (WzImage img in imagesToUnparse)
                img.UnparseImage();
            imagesToUnparse.Clear();*/
        }

        public void SerializeFile(WzFile file, string path)
        {
            SerializeObject(file, path);
        }

        public void SerializeDirectory(WzDirectory file, string path)
        {
            SerializeObject(file, path);
        }

        public void SerializeImage(WzImage file, string path)
        {
            SerializeObject(file, path);
        }

        private int CalculateTotal(WzObject currObj)
        {
            int result = 0;
            if (currObj is WzFile)
                result += ((WzFile)currObj).WzDirectory.CountImages();
            else if (currObj is WzDirectory)
                result += ((WzDirectory)currObj).CountImages();
            return result;
        }

        private void ExportRecursion(WzObject currObj, string outPath)
        {
            if (currObj is WzFile)
                ExportRecursion(((WzFile)currObj).WzDirectory, outPath);
            else if (currObj is WzDirectory)
            {
                outPath += currObj.Name + @"\";
                if (!Directory.Exists(outPath)) Directory.CreateDirectory(outPath);
                foreach (WzDirectory subdir in ((WzDirectory)currObj).WzDirectories)
                    ExportRecursion(subdir, outPath + subdir.Name + @"\");
                foreach (WzImage subimg in ((WzDirectory)currObj).WzImages)
                    ExportRecursion(subimg, outPath + subimg.Name + @"\");
            }
            else if (currObj is WzCanvasProperty)
            {
                Bitmap bmp = ((WzCanvasProperty)currObj).PngProperty.GetPNG(false);
                string path = outPath + currObj.Name + ".png";
                bmp.Save(path, ImageFormat.Png);
                //curr++;
            }
            else if (currObj is WzSoundProperty)
            {
                string path = outPath + currObj.Name + ".mp3";
                ((WzSoundProperty)currObj).SaveToFile(path);
            }
            else if (currObj is WzImage)
            {
                outPath += currObj.Name + @"\";
                if (!Directory.Exists(outPath)) Directory.CreateDirectory(outPath);
                bool parse = ((WzImage)currObj).Parsed || ((WzImage)currObj).Changed;
                if (!parse) ((WzImage)currObj).ParseImage();
                foreach (WzImageProperty subprop in ((IPropertyContainer)currObj).WzProperties)
                    ExportRecursion(subprop, outPath);
                if (!parse) ((WzImage)currObj).UnparseImage();
                curr++;
            }
            else if (currObj is IPropertyContainer)
            {
                outPath += currObj.Name + ".";
                foreach (WzImageProperty subprop in ((IPropertyContainer)currObj).WzProperties)
                    ExportRecursion(subprop, outPath);
            }
            else if (currObj is WzUOLProperty)
                ExportRecursion(((WzUOLProperty)currObj).LinkValue, outPath);
        }
    }

    public class WzJSONSerializer : WzJsonSerializer, IWzDirectorySerializer
    {
        public WzJSONSerializer()
        { }

        //Creates folder with WZ file name
        private void exportDirJSONInternal(WzDirectory dir, string path)
        {
            if (!Directory.Exists(path))
                createDirSafe(ref path);

            if (path.Substring(path.Length - 1) != @"\")
                path += @"\";

            switch (dir.name)
            {
                case "Item.wz":
                    ItemToJSON(dir, path);
                    break;
                case "String.wz":
                    StringToJSON(dir, path);
                    break;
                case "Character.wz":
                    CharacterToJSON(dir, path);
                    break;
                case "Mob.wz":
                case "Mob2.wz":
                    MobToJSON(dir, path);
                    break;
                case "Map.wz":
                    MapToJSON(dir, path);
                    break;
                case "Npc.wz":
                    NpcToJSON(dir, path);
                    break;
            }
        }

        public void SerializeDirectory(WzDirectory dir, string path)
        {
            total = dir.CountImages(); curr = 0;
            exportDirJSONInternal(dir, path);
        }

        public void SerializeFile(WzFile file, string path)
        {
            SerializeDirectory(file.WzDirectory, path);
        }

        private void NpcToJSON(WzDirectory dir, string path)
        {
            dir.images.ForEach(npc =>
            {
                string icon = "";
                string link = "";
                if (npc["link"] != null)
                {
                    link = npc["link"].ReadString("");
                }
                try
                {
                    MemoryStream stream = new MemoryStream();
                    WzCanvasProperty img = (WzCanvasProperty)npc["stand"]["0"];
                    img.PngProperty.GetPNG(false).Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                    byte[] pngbytes = stream.ToArray();
                    stream.Close();
                    icon = Convert.ToBase64String(pngbytes);
                }
                catch (Exception ex)
                {
                    //No image available.
                }

                using (TextWriter tw = new StreamWriter(File.Create(path + npc.Name.Replace(".img", "") + ".json")))
                {
                    tw.Write("{\"link\":\"" + link + "\",\"icon\":\"" + icon + "\"}");
                }
            });
        }

        private void MapToJSON(WzDirectory dir, string path)
        {
            WzObject[] sections =
                {
                dir["Map"]["Map0"],
                dir["Map"]["Map1"],
                dir["Map"]["Map2"],
                dir["Map"]["Map3"],
                dir["Map"]["Map4"],
                dir["Map"]["Map5"],
                dir["Map"]["Map6"],
                dir["Map"]["Map7"],
                dir["Map"]["Map8"],
                dir["Map"]["Map9"]
                };

            foreach (WzObject section in sections)
            {
                ((WzDirectory)section).WzImages.ForEach(map =>
                {
                    WzSubProperty info = (WzSubProperty)map["info"];
                    WzSubProperty portal = (WzSubProperty)map["portal"];
                    WzSubProperty reactor = (WzSubProperty)map["reactor"];
                    WzSubProperty life = (WzSubProperty)map["life"];

                    string icon = "";
                    try
                    {
                        MemoryStream stream = new MemoryStream();
                        WzImageProperty miniMap = map["miniMap"];
                        WzCanvasProperty img = (WzCanvasProperty)miniMap["canvas"];
                        img.PngProperty.GetPNG(false).Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                        byte[] pngbytes = stream.ToArray();
                        stream.Close();
                        icon = Convert.ToBase64String(pngbytes);
                    }
                    catch (Exception ex)
                    {
                        //No minimap image available.
                    }

                    string bgm = info["bgm"] == null ? "" : info["bgm"].ReadString("");
                    int cloud = info["cloud"] == null ? 0 : info["cloud"].ReadValue();
                    int fieldLimit = info["fieldLimit"] == null ? 0 : info["fieldLimit"].ReadValue();
                    string fieldScript = info["fieldScript"] == null ? "" : info["fieldScript"].ReadString("");
                    int fly = info["fly"] == null ? 0 : info["fly"].ReadValue();
                    int forcedReturn = info["forcedReturn"] == null ? 0 : info["forcedReturn"].ReadValue();
                    int hideMinimap = info["hideMinimap"] == null ? 0 : info["hideMinimap"].ReadValue();
                    string mapDesc = info["mapDesc"] == null ? "" : info["mapDesc"].ReadString("");
                    string mapMark = info["mapMark"] == null ? "" : info["mapMark"].ReadString("");
                    int mobRate = info["mobRate"] == null ? 0 : info["mobRate"].ReadValue();
                    int moveLimit = info["moveLimit"] == null ? 0 : info["moveLimit"].ReadValue();
                    int noMapCmd = info["noMapCmd"] == null ? 0 : info["noMapCmd"].ReadValue();
                    string onFirstUserEnter = info["onFirstUserEnter"] == null ? "" : info["onFirstUserEnter"].ReadString("");
                    string onUserEnter = info["onUserEnter"] == null ? "" : info["onUserEnter"].ReadString("");
                    int returnMap = info["returnMap"] == null ? 0 : info["returnMap"].ReadValue();
                    int swim = info["swim"] == null ? 0 : info["swim"].ReadValue();
                    int town = info["town"] == null ? 0 : info["town"].ReadValue();
                    int version = info["version"] == null ? 0 : info["version"].ReadValue();
                    int standAlone = info["standAlone"] == null ? 0 : info["standAlone"].ReadValue();
                    int partyStandAlone = info["partyStandAlone"] == null ? 0 : info["partyStandAlone"].ReadValue();

                    string reactors = "[";
                    if (reactor != null && reactor.WzProperties != null && reactor.WzProperties.Count() > 0)
                    {
                        reactor.WzProperties.ForEach(reactorEntry =>
                        {
                            reactors += "{\"id\":" + reactorEntry["id"].ReadValue() + ",";
                            reactors += "\"f\":" + reactorEntry["f"].ReadValue() + ",";
                            reactors += "\"name\":\"" + reactorEntry["name"].ReadString("") + "\",";
                            reactors += "\"reactorTime\":" + reactorEntry["reactorTime"].ReadValue() + ",";
                            reactors += "\"x\":" + reactorEntry["x"].ReadValue() + ",";
                            reactors += "\"y\":" + reactorEntry["y"].ReadValue() + "},";
                        });
                        reactors = reactors.Substring(0, reactors.Length - 1) + "]";
                    }
                    else
                    {
                        reactors += "]";
                    }

                    string portals = "[";
                    if (portal != null && portal.WzProperties != null && portal.WzProperties.Count() > 0)
                    {
                        portal.WzProperties.ForEach(portalEntry =>
                        {
                            portals += "{\"pn\":\"" + portalEntry["pn"].ReadString("") + "\",";
                            portals += "\"pt\":" + portalEntry["pt"].ReadValue() + ",";
                            portals += "\"tm\":" + portalEntry["tm"].ReadValue() + ",";
                            portals += "\"tn\":\"" + portalEntry["tn"].ReadString("") + "\",";
                            portals += "\"delay\":" + (portalEntry["delay"] == null ? 0 : portalEntry["delay"].ReadValue()) + ",";
                            portals += "\"hideTooltip\":" + (portalEntry["hideTooltip"] == null ? 0 : portalEntry["hideTooltip"].ReadValue()) + ",";
                            portals += "\"onlyOnce\":" + (portalEntry["onlyOnce"] == null ? 0 : portalEntry["onlyOnce"].ReadValue()) + ",";
                            portals += "\"script\":\"" + (portalEntry["script"] == null ? "" : portalEntry["script"].ReadString("")) + "\",";
                            portals += "\"x\":" + portalEntry["x"].ReadValue() + ",";
                            portals += "\"y\":" + portalEntry["y"].ReadValue() + "},";
                        });
                        portals = portals.Substring(0, portals.Length - 1) + "]";
                    }
                    else
                    {
                        portals += "]";
                    }

                    string lifes = "[";
                    if (life != null && life.WzProperties != null && life.WzProperties.Count() > 0)
                    {
                        life.WzProperties.ForEach(lifeEntry =>
                        {
                            lifes += "{\"id\":" + lifeEntry["id"].ReadValue() + ",";
                            lifes += "\"cy\":" + lifeEntry["cy"].ReadValue() + ",";
                            lifes += "\"fh\":" + lifeEntry["fh"].ReadValue() + ",";
                            lifes += "\"rx0\":" + lifeEntry["rx0"].ReadValue() + ",";
                            lifes += "\"rx1\":" + lifeEntry["rx1"].ReadValue() + ",";
                            lifes += "\"type\":\"" + lifeEntry["type"].ReadString("") + "\",";
                            lifes += "\"x\":" + lifeEntry["x"].ReadValue() + ",";
                            lifes += "\"y\":" + lifeEntry["y"].ReadValue() + "},";
                        });
                        lifes = lifes.Substring(0, lifes.Length - 1) + "]";
                    }
                    else
                    {
                        lifes += "]";
                    }

                    using (TextWriter tw = new StreamWriter(File.Create(path + int.Parse(map.Name.Replace(".img", "")) + ".json")))
                    {
                        tw.Write("{\"icon\":\"" + icon + "\",");
                        tw.Write("\"bgm\":\"" + bgm + "\",");
                        tw.Write("\"cloud\":" + cloud + ",");
                        tw.Write("\"fieldLimit\":" + fieldLimit + ",");
                        tw.Write("\"fieldScript\":\"" + fieldScript + "\",");
                        tw.Write("\"fly\":" + fly + ",");
                        tw.Write("\"forcedReturn\":" + forcedReturn + ",");
                        tw.Write("\"hideMinimap\":" + hideMinimap + ",");
                        tw.Write("\"mapDesc\":\"" + mapDesc + "\",");
                        tw.Write("\"mapMark\":\"" + mapMark + "\",");
                        tw.Write("\"mobRate\":" + mobRate + ",");
                        tw.Write("\"moveLimit\":" + moveLimit + ",");
                        tw.Write("\"noMapCmd\":" + noMapCmd + ",");
                        tw.Write("\"onFirstUserEnter\":\"" + onFirstUserEnter + "\",");
                        tw.Write("\"onUserEnter\":\"" + onUserEnter + "\",");
                        tw.Write("\"returnMap\":" + returnMap + ",");
                        tw.Write("\"swim\":" + swim + ",");
                        tw.Write("\"town\":" + town + ",");
                        tw.Write("\"version\":" + version + ",");
                        tw.Write("\"standAlone\":" + standAlone + ",");
                        tw.Write("\"partyStandAlone\":" + partyStandAlone + ",");
                        tw.Write("\"portal\":" + portals + ",");
                        tw.Write("\"life\":" + lifes + ",");
                        tw.Write("\"reactor\":" + reactors + "}");
                    }
                });
            }
        }

        private void MobToJSON(WzDirectory dir, string path)
        {
            foreach (WzImage mob in dir.WzImages)
            {
                WzSubProperty info = (WzSubProperty)mob["info"];

                string icon = "";
                string link = "0";

                //Resolve icon
                if (info["link"] != null) //Link has no icon
                {
                    link = info["link"].ReadString("");
                }
                else if (info["skeleton"] == null) //Skels have no icons
                {
                    if (info["thumbnail"] != null) //Zakum altar has thumbnail instead of stand.
                    {
                        MemoryStream stream = new MemoryStream();
                        if (info["thumbnail"].PropertyType == WzPropertyType.Canvas)
                        {
                            WzCanvasProperty img = (WzCanvasProperty)info["thumbnail"];
                            if (img == null) continue;
                            img.PngProperty.GetPNG(false).Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                        }
                        else if (info["thumbnail"].PropertyType == WzPropertyType.UOL)
                        {
                            WzUOLProperty uol = (WzUOLProperty)info["thumbnail"];
                            WzCanvasProperty img = (WzCanvasProperty)uol.LinkValue;
                            if (img == null) continue;
                            img.PngProperty.GetPNG(false).Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                        }

                        byte[] pngbytes = stream.ToArray();
                        stream.Close();
                        icon = Convert.ToBase64String(pngbytes);
                    }
                    else if (mob["stand"] != null && mob["stand"]["0"] != null)
                    {
                        MemoryStream stream = new MemoryStream();
                        if (mob["stand"]["0"].PropertyType == WzPropertyType.Canvas)
                        {
                            WzCanvasProperty img = (WzCanvasProperty)mob["stand"]["0"];
                            if (img == null) continue;
                            img.PngProperty.GetPNG(false).Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                        }
                        else if (mob["stand"]["0"].PropertyType == WzPropertyType.UOL)
                        {
                            WzUOLProperty uol = (WzUOLProperty)mob["stand"]["0"];
                            WzCanvasProperty img = (WzCanvasProperty)uol.LinkValue;
                            if (img == null) continue;
                            img.PngProperty.GetPNG(false).Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                        }

                        byte[] pngbytes = stream.ToArray();
                        stream.Close();
                        icon = Convert.ToBase64String(pngbytes);
                    }
                    else if (mob["stand"] != null && mob["stand"]["123"] != null)
                    {
                        MemoryStream stream = new MemoryStream();
                        if (mob["stand"]["123"].PropertyType == WzPropertyType.Canvas)
                        {
                            WzCanvasProperty img = (WzCanvasProperty)mob["stand"]["123"];
                            if (img == null) continue;
                            img.PngProperty.GetPNG(false).Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                        }
                        else if (mob["stand"]["123"].PropertyType == WzPropertyType.UOL)
                        {
                            WzUOLProperty uol = (WzUOLProperty)mob["stand"]["123"];
                            WzCanvasProperty img = (WzCanvasProperty)uol.LinkValue;
                            if (img == null) continue;
                            img.PngProperty.GetPNG(false).Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                        }

                        byte[] pngbytes = stream.ToArray();
                        stream.Close();
                        icon = Convert.ToBase64String(pngbytes);
                    }
                    else if (mob["stand"] != null && mob["stand"]["3"] != null)
                    {
                        MemoryStream stream = new MemoryStream();
                        if (mob["stand"]["3"].PropertyType == WzPropertyType.Canvas)
                        {
                            WzCanvasProperty img = (WzCanvasProperty)mob["stand"]["3"];
                            if (img == null) continue;
                            img.PngProperty.GetPNG(false).Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                        }
                        else if (mob["stand"]["3"].PropertyType == WzPropertyType.UOL)
                        {
                            WzUOLProperty uol = (WzUOLProperty)mob["stand"]["3"];
                            WzCanvasProperty img = (WzCanvasProperty)uol.LinkValue;
                            if (img == null) continue;
                            img.PngProperty.GetPNG(false).Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                        }

                        byte[] pngbytes = stream.ToArray();
                        stream.Close();
                        icon = Convert.ToBase64String(pngbytes);
                    }
                    else if (mob["fly"] != null && mob["fly"]["0"] != null)
                    {
                        MemoryStream stream = new MemoryStream();
                        if (mob["fly"]["0"].PropertyType == WzPropertyType.Canvas)
                        {
                            WzCanvasProperty img = (WzCanvasProperty)mob["fly"]["0"];
                            if (img == null) continue;
                            img.PngProperty.GetPNG(false).Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                        }
                        else if (mob["fly"]["0"].PropertyType == WzPropertyType.UOL)
                        {
                            WzUOLProperty uol = (WzUOLProperty)mob["fly"]["0"];
                            WzCanvasProperty img = (WzCanvasProperty)uol.LinkValue;
                            if (img == null) continue;
                            img.PngProperty.GetPNG(false).Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                        }

                        byte[] pngbytes = stream.ToArray();
                        stream.Close();
                        icon = Convert.ToBase64String(pngbytes);
                    }
                    else if (mob["fly"] != null && mob["fly"]["001"] != null)
                    {
                        MemoryStream stream = new MemoryStream();
                        if (mob["fly"]["001"].PropertyType == WzPropertyType.Canvas)
                        {
                            WzCanvasProperty img = (WzCanvasProperty)mob["fly"]["001"];
                            if (img == null) continue;
                            img.PngProperty.GetPNG(false).Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                        }
                        else if (mob["fly"]["001"].PropertyType == WzPropertyType.UOL)
                        {
                            WzUOLProperty uol = (WzUOLProperty)mob["fly"]["001"];
                            WzCanvasProperty img = (WzCanvasProperty)uol.LinkValue;
                            if (img == null) continue;
                            img.PngProperty.GetPNG(false).Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                        }

                        byte[] pngbytes = stream.ToArray();
                        stream.Close();
                        icon = Convert.ToBase64String(pngbytes);
                    }
                    else
                    {
                        MessageBox.Show("No icon or link found for Monster.", "Error: " + mob.name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

                int acc = info["acc"] == null ? 0 : info["acc"].ReadValue();
                int bodyAttack = info["bodyAttack"] == null ? 0 : info["bodyAttack"].ReadValue();
                int boss = info["boss"] == null ? 0 : info["boss"].ReadValue();
                int category = info["category"] == null ? 0 : info["category"].ReadValue();
                int charismaEXP = info["charismaEXP"] == null ? 0 : info["charismaEXP"].ReadValue();
                int eva = info["eva"] == null ? 0 : info["eva"].ReadValue();
                int firstAttack = info["firstAttack"] == null ? 0 : info["firstAttack"].ReadValue();
                int fs = info["fs"] == null ? 0 : info["fs"].ReadValue();
                int hpRecovery = info["hpRecovery"] == null ? 0 : info["hpRecovery"].ReadValue();
                int hpTagBgcolor = info["hpTagBgcolor"] == null ? 0 : info["hpTagBgcolor"].ReadValue();
                int hpTagColor = info["hpTagColor"] == null ? 0 : info["hpTagColor"].ReadValue();
                int level = info["level"] == null ? 0 : info["level"].ReadValue();
                int MADamage = info["MADamage"] == null ? 0 : info["MADamage"].ReadValue();
                long defaultHP = info["defaultHP"] == null ? 0 : info["defaultHP"].ReadLong();
                long defaultMP = info["defaultMP"] == null ? 0 : info["defaultMP"].ReadLong();
                long maxHP = info["maxHP"] == null ? 0 : info["maxHP"].ReadLong();
                long maxMP = info["maxMP"] == null ? 0 : info["maxMP"].ReadLong();
                long finalmaxHP = info["finalmaxHP"] == null ? 0 : info["finalmaxHP"].ReadLong();
                int mbookID = info["mbookID"] == null ? 0 : info["mbookID"].ReadValue();
                int MDDamage = info["MDDamage"] == null ? 0 : info["MDDamage"].ReadValue();
                int MDRate = info["MDRate"] == null ? 0 : info["MDRate"].ReadValue();
                string mobType = info["mobType"] == null ? "" : info["mobType"].ReadString("");
                int mpRecovery = info["mpRecovery"] == null ? 0 : info["mpRecovery"].ReadValue();
                int noFlip = info["noFlip"] == null ? 0 : info["noFlip"].ReadValue();
                int PADamage = info["PADamage"] == null ? 0 : info["PADamage"].ReadValue();
                int PDDamage = info["PDDamage"] == null ? 0 : info["PDDamage"].ReadValue();
                int PDRate = info["PDRate"] == null ? 0 : info["PDRate"].ReadValue();
                int pushed = info["pushed"] == null ? 0 : info["pushed"].ReadValue();
                int summonType = info["summonType"] == null ? 0 : info["summonType"].ReadValue();
                string elemAttr = info["elemAttr"] == null ? "" : info["elemAttr"].ReadString("");
                long exp = info["exp"] == null ? 0 : info["exp"].ReadLong();
                int explosiveReward = info["explosiveReward"] == null ? 0 : info["explosiveReward"].ReadValue();
                int ignoreFieldOut = info["ignoreFieldOut"] == null ? 0 : info["ignoreFieldOut"].ReadValue();
                int ignoreMoveImpact = info["ignoreMoveImpact"] == null ? 0 : info["ignoreMoveImpact"].ReadValue();
                int individualReward = info["individualReward"] == null ? 0 : info["individualReward"].ReadValue();
                int overSpeed = info["overSpeed"] == null ? 0 : info["overSpeed"].ReadValue();
                int useReaction = info["useReaction"] == null ? 0 : info["useReaction"].ReadValue();
                int wp = info["wp"] == null ? 0 : info["wp"].ReadValue();
                int invincible = info["invincible"] == null ? 0 : info["invincible"].ReadValue();
                int fixedDamage = info["fixedDamage"] == null ? 0 : info["fixedDamage"].ReadValue();
                int HPgaugeHide = info["HPgaugeHide"] == null ? 0 : info["HPgaugeHide"].ReadValue();
                int PassiveDisease = info["PassiveDisease"] == null ? 0 : info["PassiveDisease"].ReadValue();
                int PartyBonusMob = info["PartyBonusMob"] == null ? 0 : info["PartyBonusMob"].ReadValue();
                int showNotRemoteDam = info["showNotRemoteDam"] == null ? 0 : info["showNotRemoteDam"].ReadValue();
                int hideName = info["hideName"] == null ? 0 : info["hideName"].ReadValue();
                int changeableMob = info["changeableMob"] == null ? 0 : info["changeableMob"].ReadValue();

                //Revive: mobs spawned upon death.
                string revive = "[";
                if (info["revive"] != null)
                {
                    info["revive"].WzProperties.ForEach(elem =>
                    {
                        if (elem.PropertyType == WzPropertyType.String)
                        {
                            revive += ((WzStringProperty)elem).ReadString("") + ",";
                        }
                        else
                        {
                            revive += elem.ReadValue() + ",";
                        }
                    });
                    revive = revive.Substring(0, revive.Length - 1);
                }
                revive += "]";

                //Skills
                string skills = "[";
                if (info["skill"] != null)
                {
                    foreach (WzSubProperty skill in info["skill"].WzProperties)
                    {
                        skills += "{";
                        skills += "\"action\":" + skill["action"].ReadValue() + ",";
                        skills += "\"info\":\"" + Regex.Replace(System.Security.SecurityElement.Escape(skill["info"].ReadString("")), @"\r\n?|\n", " ") + "\",";
                        skills += "\"level\":" + skill["level"].ReadValue() + ",";
                        skills += "\"skill\":" + skill["skill"].ReadValue() + "},";
                    }
                    skills = skills.Substring(0, skills.Length - 1);
                }
                skills += "]";

                string JSON = "{";
                JSON += "\"id\":" + int.Parse(mob.name.Replace(".img", "")) + ",";
                JSON += "\"icon\":\"" + icon + "\",";
                JSON += "\"link\":" + link + ",";

                JSON += "\"acc\":" + acc + ",";
                JSON += "\"bodyAttack\":" + bodyAttack + ",";
                JSON += "\"boss\":" + boss + ",";
                JSON += "\"category\":" + category + ",";
                JSON += "\"charismaEXP\":" + charismaEXP + ",";
                JSON += "\"eva\":" + eva + ",";
                JSON += "\"firstAttack\":" + firstAttack + ",";
                JSON += "\"fs\":" + fs + ",";
                JSON += "\"hpRecovery\":" + hpRecovery + ",";
                JSON += "\"hpTagBgcolor\":" + hpTagBgcolor + ",";
                JSON += "\"hpTagColor\":" + hpTagColor + ",";
                JSON += "\"level\":" + level + ",";
                JSON += "\"MADamage\":" + MADamage + ",";
                JSON += "\"defaultHP\":" + defaultHP + ",";
                JSON += "\"defaultMP\":" + defaultMP + ",";
                JSON += "\"maxHP\":" + maxHP + ",";
                JSON += "\"maxMP\":" + maxMP + ",";
                JSON += "\"finalmaxHP\":" + finalmaxHP + ",";
                JSON += "\"mbookID\":" + mbookID + ",";
                JSON += "\"MDDamage\":" + MDDamage + ",";
                JSON += "\"MDRate\":" + MDRate + ",";
                JSON += "\"mobType\":\"" + mobType + "\",";
                JSON += "\"mpRecovery\":" + mpRecovery + ",";
                JSON += "\"noFlip\":" + noFlip + ",";
                JSON += "\"PADamage\":" + PADamage + ",";
                JSON += "\"PDDamage\":" + PDDamage + ",";
                JSON += "\"PDRate\":" + PDRate + ",";
                JSON += "\"pushed\":" + pushed + ",";
                JSON += "\"summonType\":" + summonType + ",";
                JSON += "\"elemAttr\":\"" + elemAttr + "\",";
                JSON += "\"exp\":" + exp + ",";
                JSON += "\"explosiveReward\":" + explosiveReward + ",";
                JSON += "\"ignoreFieldOut\":" + ignoreFieldOut + ",";
                JSON += "\"ignoreMoveImpact\":" + ignoreMoveImpact + ",";
                JSON += "\"individualReward\":" + individualReward + ",";
                JSON += "\"overSpeed\":" + overSpeed + ",";
                JSON += "\"useReaction\":" + useReaction + ",";
                JSON += "\"wp\":" + wp + ",";
                JSON += "\"invincible\":" + invincible + ",";
                JSON += "\"fixedDamage\":" + fixedDamage + ",";
                JSON += "\"HPgaugeHide\":" + HPgaugeHide + ",";
                JSON += "\"PassiveDisease\":" + PassiveDisease + ",";
                JSON += "\"PartyBonusMob\":" + PartyBonusMob + ",";
                JSON += "\"showNotRemoteDam\":" + showNotRemoteDam + ",";
                JSON += "\"hideName\":" + hideName + ",";
                JSON += "\"changeableMob\":" + changeableMob + ",";
                JSON += "\"revive\":" + revive + ",";
                JSON += "\"skill\":" + skills + "}";

                using (TextWriter tw = new StreamWriter(File.Create(path + int.Parse(mob.name.Replace(".img", "")) + ".json")))
                {
                    tw.Write(JSON);
                }

            }
        }

        private void CharacterToJSON(WzDirectory dir, string path)
        {
            //Categories
            WzDirectory[] categories = {
                (WzDirectory)dir["Accessory"],
                (WzDirectory)dir["Android"],
                (WzDirectory)dir["ArcaneForce"],
                (WzDirectory)dir["Bits"],
                (WzDirectory)dir["Cap"],
                (WzDirectory)dir["Cape"],
                (WzDirectory)dir["Coat"],
                (WzDirectory)dir["Dragon"],
                (WzDirectory)dir["Face"],
                (WzDirectory)dir["Familiar"],
                (WzDirectory)dir["Glove"],
                (WzDirectory)dir["Hair"],
                (WzDirectory)dir["Longcoat"],
                (WzDirectory)dir["Mechanic"],
                (WzDirectory)dir["MonsterBook"],
                (WzDirectory)dir["Pants"],
                (WzDirectory)dir["PetEquip"],
                (WzDirectory)dir["Ring"],
                (WzDirectory)dir["Shield"],
                (WzDirectory)dir["Shoes"],
                (WzDirectory)dir["TamingMob"],
                (WzDirectory)dir["Totem"],
                (WzDirectory)dir["Weapon"]
            };

            HashSet<string> props = new HashSet<string>();
            foreach (WzDirectory cat in categories)
            {
                foreach (WzImage item in cat.WzImages)
                {
                    String JSON = "{";
                    JSON += "\"id\":\"" + int.Parse(item.name.Replace(".img", "")) + "\",";

                    WzSubProperty info = (WzSubProperty)item["info"];

                    //Standard Values
                    JSON += "\"cash\":\"" + info["cash"].ReadValue() + "\",";
                    JSON += "\"iSlot\":\"" + info["iSlot"].ReadString("") + "\",";
                    JSON += "\"vSlot\":\"" + info["vSlot"].ReadString("") + "\",";
                    JSON += "\"reqJob\":\"" + info["reqJob"].ReadValue() + "\",";
                    JSON += "\"reqLevel\":\"" + info["reqLevel"].ReadValue() + "\",";
                    JSON += "\"reqSTR\":\"" + info["reqSTR"].ReadValue() + "\",";
                    JSON += "\"reqDEX\":\"" + info["reqDEX"].ReadValue() + "\",";
                    JSON += "\"reqINT\":\"" + info["reqINT"].ReadValue() + "\",";
                    JSON += "\"reqLUK\":\"" + info["reqLUK"].ReadValue() + "\",";

                    MemoryStream stream = new MemoryStream();
                    if (info["icon"] != null)
                    {
                        if (info["icon"].PropertyType == WzPropertyType.Canvas)
                        {
                            WzCanvasProperty img = (WzCanvasProperty)info["icon"];
                            if (img == null) continue;
                            img.PngProperty.GetPNG(false).Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                        }
                        else if (info["icon"].PropertyType == WzPropertyType.UOL)
                        {
                            WzUOLProperty uol = (WzUOLProperty)info["icon"];
                            WzCanvasProperty img = (WzCanvasProperty)uol.LinkValue;
                            if (img == null) continue;
                            img.PngProperty.GetPNG(false).Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                        }
                    }

                    byte[] pngbytes = stream.ToArray();
                    stream.Close();
                    JSON += "\"icon\":\"" + Convert.ToBase64String(pngbytes) + "\",";

                    int setItemID = info["setItemID"] == null ? 0 : info["setItemID"].ReadValue();
                    int tuc = info["tuc"] == null ? 0 : info["tuc"].ReadValue();
                    int price = info["price"] == null ? 0 : info["price"].ReadValue();
                    int notSale = info["notSale"] == null ? 0 : info["notSale"].ReadValue();
                    int only = info["only"] == null ? 0 : info["only"].ReadValue();
                    int tradeBlock = info["tradeBlock"] == null ? 0 : info["tradeBlock"].ReadValue();
                    int onlyEquip = info["onlyEquip"] == null ? 0 : info["onlyEquip"].ReadValue();
                    int addition = info["addition"] == null ? 0 : info["addition"].ReadValue();
                    int incPDD = info["incPDD"] == null ? 0 : info["incPDD"].ReadValue();
                    int incMDD = info["incMDD"] == null ? 0 : info["incMDD"].ReadValue();
                    int incPAD = info["incPAD"] == null ? 0 : info["incPAD"].ReadValue();
                    int incMAD = info["incMAD"] == null ? 0 : info["incMAD"].ReadValue();
                    int incSTR = info["incSTR"] == null ? 0 : info["incSTR"].ReadValue();
                    int incINT = info["incINT"] == null ? 0 : info["incINT"].ReadValue();
                    int incDEX = info["incDEX"] == null ? 0 : info["incDEX"].ReadValue();
                    int incLUK = info["incLUK"] == null ? 0 : info["incLUK"].ReadValue();
                    int superiorEqp = info["superiorEqp"] == null ? 0 : info["superiorEqp"].ReadValue();
                    int tradeAvailable = info["tradeAvailable"] == null ? 0 : info["tradeAvailable"].ReadValue();
                    int exItem = info["exItem"] == null ? 0 : info["exItem"].ReadValue();
                    int incMHP = info["incMHP"] == null ? 0 : info["incMHP"].ReadValue();
                    int incMMP = info["incMMP"] == null ? 0 : info["incMMP"].ReadValue();
                    int medalTag = info["medalTag"] == null ? 0 : info["medalTag"].ReadValue();
                    int incACC = info["incACC"] == null ? 0 : info["incACC"].ReadValue();
                    int incEVA = info["incEVA"] == null ? 0 : info["incEVA"].ReadValue();
                    int timeLimited = info["timeLimited"] == null ? 0 : info["timeLimited"].ReadValue();
                    int incSpeed = info["incSpeed"] == null ? 0 : info["incSpeed"].ReadValue();
                    int fixedPotential = info["fixedPotential"] == null ? 0 : info["fixedPotential"].ReadValue();
                    int fixedGrade = info["fixedGrade"] == null ? 0 : info["fixedGrade"].ReadValue();
                    int specialGrade = info["specialGrade"] == null ? 0 : info["specialGrade"].ReadValue();
                    int option = info["option"] == null ? 0 : info["option"].ReadValue();
                    int charismaEXP = info["charismaEXP"] == null ? 0 : info["charismaEXP"].ReadValue();
                    int charmEXP = info["charmEXP"] == null ? 0 : info["charmEXP"].ReadValue();
                    int willEXP = info["willEXP"] == null ? 0 : info["willEXP"].ReadValue();
                    int notExtend = info["notExtend"] == null ? 0 : info["notExtend"].ReadValue();
                    int exGrade = info["exGrade"] == null ? 0 : info["exGrade"].ReadValue();
                    int level = info["level"] == null ? 0 : info["level"].ReadValue();
                    int incJump = info["incJump"] == null ? 0 : info["incJump"].ReadValue();
                    int bossReward = info["bossReward"] == null ? 0 : info["bossReward"].ReadValue();
                    int sharableOnce = info["sharableOnce"] == null ? 0 : info["sharableOnce"].ReadValue();
                    int accountSharable = info["accountSharable"] == null ? 0 : info["accountSharable"].ReadValue();
                    int baseLevel = info["baseLevel"] == null ? 0 : info["baseLevel"].ReadValue();
                    int abilityTimeLimited = info["abilityTimeLimited"] == null ? 0 : info["abilityTimeLimited"].ReadValue();
                    int incMHPr = info["incMHPr"] == null ? 0 : info["incMHPr"].ReadValue();
                    int incMMPr = info["incMMPr"] == null ? 0 : info["incMMPr"].ReadValue();
                    int equipTradeBlock = info["equipTradeBlock"] == null ? 0 : info["equipTradeBlock"].ReadValue();
                    int bonusExp = info["bonusExp"] == null ? 0 : info["bonusExp"].ReadValue();
                    int reduceReq = info["reduceReq"] == null ? 0 : info["reduceReq"].ReadValue();
                    int incCraft = info["incCraft"] == null ? 0 : info["incCraft"].ReadValue();
                    int scope = info["scope"] == null ? 0 : info["scope"].ReadValue();
                    int accountShareTag = info["accountShareTag"] == null ? 0 : info["accountShareTag"].ReadValue();
                    int bdR = info["bdR"] == null ? 0 : info["bdR"].ReadValue();
                    int imdR = info["imdR"] == null ? 0 : info["imdR"].ReadValue();
                    int noPotential = info["noPotential"] == null ? 0 : info["noPotential"].ReadValue();
                    int dayOfWeekItemStat = info["dayOfWeekItemStat"] == null ? 0 : info["dayOfWeekItemStat"].ReadValue();
                    int slotMax = info["slotMax"] == null ? 0 : info["slotMax"].ReadValue();
                    int noExpend = info["noExpend"] == null ? 0 : info["noExpend"].ReadValue();
                    int specialID = info["specialID"] == null ? 0 : info["specialID"].ReadValue();
                    int invisibleFace = info["invisibleFace"] == null ? 0 : info["invisibleFace"].ReadValue();
                    int noMoveToLocker = info["noMoveToLocker"] == null ? 0 : info["noMoveToLocker"].ReadValue();
                    int cashForceCharmExp = info["cashForceCharmExp"] == null ? 0 : info["cashForceCharmExp"].ReadValue();
                    int reqPOP = info["reqPOP"] == null ? 0 : info["reqPOP"].ReadValue();
                    int attackSpeed = info["attackSpeed"] == null ? 0 : info["attackSpeed"].ReadValue();
                    int TimeLimited = info["TimeLimited"] == null ? 0 : info["TimeLimited"].ReadValue();
                    int reqSpecJob = info["reqSpecJob"] == null ? 0 : info["reqSpecJob"].ReadValue();
                    int durability = info["durability"] == null ? 0 : info["durability"].ReadValue();
                    int replace = info["replace"] == null ? 0 : info["replace"].ReadValue();
                    int insightEXP = info["insightEXP"] == null ? 0 : info["insightEXP"].ReadValue();
                    int onlyUpgrade = info["onlyUpgrade"] == null ? 0 : info["onlyUpgrade"].ReadValue();
                    int epicItem = info["epicItem"] == null ? 0 : info["epicItem"].ReadValue();
                    int exceptUpgrade = info["exceptUpgrade"] == null ? 0 : info["exceptUpgrade"].ReadValue();
                    int exceptToadsHammer = info["exceptToadsHammer"] == null ? 0 : info["exceptToadsHammer"].ReadValue();
                    int exceptTransmission = info["exceptTransmission"] == null ? 0 : info["exceptTransmission"].ReadValue();
                    int equipDrop = info["equipDrop"] == null ? 0 : info["equipDrop"].ReadValue();
                    int blockGoldHammer = info["blockGoldHammer"] == null ? 0 : info["blockGoldHammer"].ReadValue();
                    int bdr = info["bdr"] == null ? 0 : info["bdr"].ReadValue();
                    int senseEXP = info["senseEXP"] == null ? 0 : info["senseEXP"].ReadValue();
                    int craftEXP = info["craftEXP"] == null ? 0 : info["craftEXP"].ReadValue();
                    int StarPlanet = info["StarPlanet"] == null ? 0 : info["StarPlanet"].ReadValue();
                    int jewelCraft = info["jewelCraft"] == null ? 0 : info["jewelCraft"].ReadValue();
                    int effect = info["effect"] == null ? 0 : info["effect"].ReadValue();
                    int tradBlock = info["tradBlock"] == null ? 0 : info["tradBlock"].ReadValue();
                    int MaxHP = info["MaxHP"] == null ? 0 : info["MaxHP"].ReadValue();
                    int PotionDiscount = info["PotionDiscount"] == null ? 0 : info["PotionDiscount"].ReadValue();
                    int cubeExBaseOptionLevel = info["cubeExBaseOptionLevel"] == null ? 0 : info["cubeExBaseOptionLevel"].ReadValue();
                    int bonusDrop = info["bonusDrop"] == null ? 0 : info["bonusDrop"].ReadValue();
                    int incCriticalMAXDamage = info["incCriticalMAXDamage"] == null ? 0 : info["incCriticalMAXDamage"].ReadValue();
                    int expireOnLogout = info["expireOnLogout"] == null ? 0 : info["expireOnLogout"].ReadValue();
                    int jokerToSetItem = info["jokerToSetItem"] == null ? 0 : info["jokerToSetItem"].ReadValue();
                    int speed = info["speed"] == null ? 0 : info["speed"].ReadValue();
                    int dropBlock = info["dropBlock"] == null ? 0 : info["dropBlock"].ReadValue();
                    int incPVPDamage = info["incPVPDamage"] == null ? 0 : info["incPVPDamage"].ReadValue();
                    int randVariation = info["randVariation"] == null ? 0 : info["randVariation"].ReadValue();
                    int incPDDr = info["incPDDr"] == null ? 0 : info["incPDDr"].ReadValue();
                    int incMDDr = info["incMDDr"] == null ? 0 : info["incMDDr"].ReadValue();
                    int incDAMr = info["incDAMr"] == null ? 0 : info["incDAMr"].ReadValue();
                    int night = info["night"] == null ? 0 : info["night"].ReadValue();
                    int CuttableCount = info["CuttableCount"] == null ? 0 : info["CuttableCount"].ReadValue();
                    string equippedEmotion = info["equippedEmotion"] == null ? "" : info["equippedEmotion"].ReadString("");
                    string equippedSound = info["equippedSound"] == null ? "" : info["equippedSound"].ReadString("");
                    int PDD = info["PDD"] == null ? 0 : info["PDD"].ReadValue();
                    int noDrop = info["noDrop"] == null ? 0 : info["noDrop"].ReadValue();
                    int incCr = info["incCr"] == null ? 0 : info["incCr"].ReadValue();
                    int android = info["android"] == null ? 0 : info["android"].ReadValue();
                    int grade = info["grade"] == null ? 0 : info["grade"].ReadValue();
                    int androidKey = info["androidKey"] == null ? 0 : info["androidKey"].ReadValue();
                    int mulVestigeCount = info["mulVestigeCount"] == null ? 0 : info["mulVestigeCount"].ReadValue();
                    int incARC = info["incARC"] == null ? 0 : info["incARC"].ReadValue();
                    int scanTradeBlock = info["scanTradeBlock"] == null ? 0 : info["scanTradeBlock"].ReadValue();
                    int reqQuestOnProgress = info["reqQuestOnProgress"] == null ? 0 : info["reqQuestOnProgress"].ReadValue();
                    int bitsSlot = info["bitsSlot"] == null ? 0 : info["bitsSlot"].ReadValue();
                    int royalSpecial = info["royalSpecial"] == null ? 0 : info["royalSpecial"].ReadValue();
                    int effectItemID = info["effectItemID"] == null ? 0 : info["effectItemID"].ReadValue();
                    int reqJob2 = info["reqJob2"] == null ? 0 : info["reqJob2"].ReadValue();
                    int origin = info["origin"] == null ? 0 : info["origin"].ReadValue();
                    int quest = info["quest"] == null ? 0 : info["quest"].ReadValue();
                    int enchantCategory = info["enchantCategory"] == null ? 0 : info["enchantCategory"].ReadValue();
                    int IUCMax = info["IUCMax"] == null ? 0 : info["IUCMax"].ReadValue();
                    int transform = info["transform"] == null ? 0 : info["transform"].ReadValue();
                    int weekly = info["weekly"] == null ? 0 : info["weekly"].ReadValue();
                    int masterSpecial = info["masterSpecial"] == null ? 0 : info["masterSpecial"].ReadValue();
                    int keywordEffect = info["keywordEffect"] == null ? 0 : info["keywordEffect"].ReadValue();
                    int extendFrame = info["extendFrame"] == null ? 0 : info["extendFrame"].ReadValue();
                    int vehicleDefaultFrame = info["vehicleDefaultFrame"] == null ? 0 : info["vehicleDefaultFrame"].ReadValue();
                    int cashTradeBlock = info["cashTradeBlock"] == null ? 0 : info["cashTradeBlock"].ReadValue();
                    int onlyCash = info["onlyCash"] == null ? 0 : info["onlyCash"].ReadValue();
                    int sample = info["sample"] == null ? 0 : info["sample"].ReadValue();
                    int lookChangeType = info["lookChangeType"] == null ? 0 : info["lookChangeType"].ReadValue();
                    int isAbleToTradeOnce = info["isAbleToTradeOnce"] == null ? 0 : info["isAbleToTradeOnce"].ReadValue();
                    int incHP = info["incHP"] == null ? 0 : info["incHP"].ReadValue();
                    int acc = info["acc"] == null ? 0 : info["acc"].ReadValue();
                    int pachinko = info["pachinko"] == null ? 0 : info["pachinko"].ReadValue();
                    int noExtend = info["noExtend"] == null ? 0 : info["noExtend"].ReadValue();
                    int addtion = info["addtion"] == null ? 0 : info["addtion"].ReadValue();
                    int groupEffectID = info["groupEffectID"] == null ? 0 : info["groupEffectID"].ReadValue();
                    int variableStat = info["variableStat"] == null ? 0 : info["variableStat"].ReadValue();
                    int undecomposable = info["undecomposable"] == null ? 0 : info["undecomposable"].ReadValue();
                    int despair = info["despair"] == null ? 0 : info["despair"].ReadValue();
                    int love = info["love"] == null ? 0 : info["love"].ReadValue();
                    int shine = info["shine"] == null ? 0 : info["shine"].ReadValue();
                    int blaze = info["blaze"] == null ? 0 : info["blaze"].ReadValue();
                    int hum = info["hum"] == null ? 0 : info["hum"].ReadValue();
                    int bowing = info["bowing"] == null ? 0 : info["bowing"].ReadValue();
                    int hot = info["hot"] == null ? 0 : info["hot"].ReadValue();
                    int range = info["range"] == null ? 0 : info["range"].ReadValue();
                    int skill = info["skill"] == null ? 0 : info["skill"].ReadValue();
                    int pad = info["pad"] == null ? 0 : info["pad"].ReadValue();
                    string FAttribute = info["FAttribute"] == null ? "" : info["FAttribute"].ReadString("");
                    string FCategory = info["FCategory"] == null ? "" : info["FCategory"].ReadString("");
                    int MobID = info["MobID"] == null ? 0 : info["MobID"].ReadValue();
                    int mob = info["mob"] == null ? 0 : info["mob"].ReadValue();
                    int noPotentialFieldtype = info["noPotentialFieldtype"] == null ? 0 : info["noPotentialFieldtype"].ReadValue();
                    int incLUk = info["incLUk"] == null ? 0 : info["incLUk"].ReadValue();
                    int recovery = info["recovery"] == null ? 0 : info["recovery"].ReadValue();
                    int linkedPairItem = info["linkedPairItem"] == null ? 0 : info["linkedPairItem"].ReadValue();
                    int pickUpBlock = info["pickUpBlock"] == null ? 0 : info["pickUpBlock"].ReadValue();
                    int pickupMeso = info["pickupMeso"] == null ? 0 : info["pickupMeso"].ReadValue();
                    int pickupItem = info["pickupItem"] == null ? 0 : info["pickupItem"].ReadValue();
                    int pickupOthers = info["pickupOthers"] == null ? 0 : info["pickupOthers"].ReadValue();
                    int sweepForDrop = info["sweepForDrop"] == null ? 0 : info["sweepForDrop"].ReadValue();
                    int longRange = info["longRange"] == null ? 0 : info["longRange"].ReadValue();
                    int consumeMP = info["consumeMP"] == null ? 0 : info["consumeMP"].ReadValue();
                    int ignorePickup = info["ignorePickup"] == null ? 0 : info["ignorePickup"].ReadValue();
                    int autoBuff = info["autoBuff"] == null ? 0 : info["autoBuff"].ReadValue();
                    int consumeHP = info["consumeHP"] == null ? 0 : info["consumeHP"].ReadValue();
                    string text = info["text"] == null ? "" : info["text"].ReadString("");
                    int textColor = info["textColor"] == null ? 0 : info["textColor"].ReadValue();
                    int textOffsetX = info["textOffsetX"] == null ? 0 : info["textOffsetX"].ReadValue();
                    int textOffsetY = info["textOffsetY"] == null ? 0 : info["textOffsetY"].ReadValue();
                    int textFontSize = info["textFontSize"] == null ? 0 : info["textFontSize"].ReadValue();
                    int textAreaX = info["textAreaX"] == null ? 0 : info["textAreaX"].ReadValue();
                    int textAreaY = info["textAreaY"] == null ? 0 : info["textAreaY"].ReadValue();
                    int consumeCure = info["consumeCure"] == null ? 0 : info["consumeCure"].ReadValue();
                    int smartPet = info["smartPet"] == null ? 0 : info["smartPet"].ReadValue();
                    int ringOptionSkill = info["ringOptionSkill"] == null ? 0 : info["ringOptionSkill"].ReadValue();
                    int ringOptionSkillLv = info["ringOptionSkillLv"] == null ? 0 : info["ringOptionSkillLv"].ReadValue();
                    int chatBalloon = info["chatBalloon"] == null ? 0 : info["chatBalloon"].ReadValue();
                    int nameTag = info["nameTag"] == null ? 0 : info["nameTag"].ReadValue();
                    int tradeblock = info["tradeblock"] == null ? 0 : info["tradeblock"].ReadValue();
                    int TradeBlock = info["TradeBlock"] == null ? 0 : info["TradeBlock"].ReadValue();
                    int expBuff = info["expBuff"] == null ? 0 : info["expBuff"].ReadValue();
                    int expRate = info["expRate"] == null ? 0 : info["expRate"].ReadValue();
                    int reqRace = info["reqRace"] == null ? 0 : info["reqRace"].ReadValue();
                    int bloodAllianceExpRate = info["bloodAllianceExpRate"] == null ? 0 : info["bloodAllianceExpRate"].ReadValue();
                    int bloodAlliancePartyExpRate = info["bloodAlliancePartyExpRate"] == null ? 0 : info["bloodAlliancePartyExpRate"].ReadValue();
                    int unchangeable = info["unchangeable"] == null ? 0 : info["unchangeable"].ReadValue();
                    int pmdR = info["pmdR"] == null ? 0 : info["pmdR"].ReadValue();
                    int hitDamRatePlus = info["hitDamRatePlus"] == null ? 0 : info["hitDamRatePlus"].ReadValue();
                    int fs = info["fs"] == null ? 0 : info["fs"].ReadValue();
                    int tamingMob = info["tamingMob"] == null ? 0 : info["tamingMob"].ReadValue();
                    int vehicleSkillIsTown = info["vehicleSkillIsTown"] == null ? 0 : info["vehicleSkillIsTown"].ReadValue();
                    int vehicleDoubleJumpLevel = info["vehicleDoubleJumpLevel"] == null ? 0 : info["vehicleDoubleJumpLevel"].ReadValue();
                    int incSwim = info["incSwim"] == null ? 0 : info["incSwim"].ReadValue();
                    int incFatigue = info["incFatigue"] == null ? 0 : info["incFatigue"].ReadValue();
                    int hpRecovery = info["hpRecovery"] == null ? 0 : info["hpRecovery"].ReadValue();
                    int mpRecovery = info["mpRecovery"] == null ? 0 : info["mpRecovery"].ReadValue();
                    int vehicleNaviFlyingLevel = info["vehicleNaviFlyingLevel"] == null ? 0 : info["vehicleNaviFlyingLevel"].ReadValue();
                    int removeBody = info["removeBody"] == null ? 0 : info["removeBody"].ReadValue();
                    int vehicleGlideLevel = info["vehicleGlideLevel"] == null ? 0 : info["vehicleGlideLevel"].ReadValue();
                    int vehicleNewFlyingLevel = info["vehicleNewFlyingLevel"] == null ? 0 : info["vehicleNewFlyingLevel"].ReadValue();
                    int ActionEffect = info["ActionEffect"] == null ? 0 : info["ActionEffect"].ReadValue();
                    int dx = info["dx"] == null ? 0 : info["dx"].ReadValue();
                    int dy = info["dy"] == null ? 0 : info["dy"].ReadValue();
                    int partsQuestID = info["partsQuestID"] == null ? 0 : info["partsQuestID"].ReadValue();
                    int partsCount = info["partsCount"] == null ? 0 : info["partsCount"].ReadValue();
                    int customVehicle = info["customVehicle"] == null ? 0 : info["customVehicle"].ReadValue();
                    int passengerNum = info["passengerNum"] == null ? 0 : info["passengerNum"].ReadValue();
                    int flip = info["flip"] == null ? 0 : info["flip"].ReadValue();
                    int walk = info["walk"] == null ? 0 : info["walk"].ReadValue();
                    int stand = info["stand"] == null ? 0 : info["stand"].ReadValue();
                    int attack = info["attack"] == null ? 0 : info["attack"].ReadValue();
                    string afterImage = info["afterImage"] == null ? "" : info["afterImage"].ReadString("");
                    string sfx = info["sfx"] == null ? "" : info["sfx"].ReadString("");
                    int head = info["head"] == null ? 0 : info["head"].ReadValue();
                    int knockback = info["knockback"] == null ? 0 : info["knockback"].ReadValue();
                    int damR = info["damR"] == null ? 0 : info["damR"].ReadValue();
                    int icnSTR = info["icnSTR"] == null ? 0 : info["icnSTR"].ReadValue();
                    int cantRepair = info["cantRepair"] == null ? 0 : info["cantRepair"].ReadValue();
                    int gatherTool = info["gatherTool"] == null ? 0 : info["gatherTool"].ReadValue();
                    int additon = info["additon"] == null ? 0 : info["additon"].ReadValue();
                    int elemDefault = info["elemDefault"] == null ? 0 : info["elemDefault"].ReadValue();
                    int incRMAI = info["incRMAI"] == null ? 0 : info["incRMAI"].ReadValue();
                    int incRMAL = info["incRMAL"] == null ? 0 : info["incRMAL"].ReadValue();
                    int incRMAF = info["incRMAF"] == null ? 0 : info["incRMAF"].ReadValue();
                    int incRMAS = info["incRMAS"] == null ? 0 : info["incRMAS"].ReadValue();
                    int epic = info["epic"] == null ? 0 : info["epic"].ReadValue();
                    int hide = info["hide"] == null ? 0 : info["hide"].ReadValue();
                    int kaiserOffsetX = info["kaiserOffsetX"] == null ? 0 : info["kaiserOffsetX"].ReadValue();
                    int kaiserOffsetY = info["kaiserOffsetY"] == null ? 0 : info["kaiserOffsetY"].ReadValue();
                    int inPAD = info["inPAD"] == null ? 0 : info["inPAD"].ReadValue();
                    int incLuk = info["incLuk"] == null ? 0 : info["incLuk"].ReadValue();
                    int incAttackCount = info["incAttackCount"] == null ? 0 : info["incAttackCount"].ReadValue();

                    JSON += "\"setItemID\":\"" + setItemID + "\",";
                    JSON += "\"tuc\":\"" + tuc + "\",";
                    JSON += "\"price\":\"" + price + "\",";
                    JSON += "\"notSale\":\"" + notSale + "\",";
                    JSON += "\"only\":\"" + only + "\",";
                    JSON += "\"tradeBlock\":\"" + tradeBlock + "\",";
                    JSON += "\"onlyEquip\":\"" + onlyEquip + "\",";
                    JSON += "\"addition\":\"" + addition + "\",";
                    JSON += "\"incPDD\":\"" + incPDD + "\",";
                    JSON += "\"incMDD\":\"" + incMDD + "\",";
                    JSON += "\"incPAD\":\"" + incPAD + "\",";
                    JSON += "\"incMAD\":\"" + incMAD + "\",";
                    JSON += "\"incSTR\":\"" + incSTR + "\",";
                    JSON += "\"incINT\":\"" + incINT + "\",";
                    JSON += "\"incDEX\":\"" + incDEX + "\",";
                    JSON += "\"incLUK\":\"" + incLUK + "\",";
                    JSON += "\"superiorEqp\":\"" + superiorEqp + "\",";
                    JSON += "\"tradeAvailable\":\"" + tradeAvailable + "\",";
                    JSON += "\"exItem\":\"" + exItem + "\",";
                    JSON += "\"incMHP\":\"" + incMHP + "\",";
                    JSON += "\"incMMP\":\"" + incMMP + "\",";
                    JSON += "\"medalTag\":\"" + medalTag + "\",";
                    JSON += "\"incACC\":\"" + incACC + "\",";
                    JSON += "\"incEVA\":\"" + incEVA + "\",";
                    JSON += "\"timeLimited\":\"" + timeLimited + "\",";
                    JSON += "\"incSpeed\":\"" + incSpeed + "\",";
                    JSON += "\"fixedPotential\":\"" + fixedPotential + "\",";
                    JSON += "\"fixedGrade\":\"" + fixedGrade + "\",";
                    JSON += "\"specialGrade\":\"" + specialGrade + "\",";
                    JSON += "\"option\":\"" + option + "\",";
                    JSON += "\"charismaEXP\":\"" + charismaEXP + "\",";
                    JSON += "\"charmEXP\":\"" + charmEXP + "\",";
                    JSON += "\"willEXP\":\"" + willEXP + "\",";
                    JSON += "\"notExtend\":\"" + notExtend + "\",";
                    JSON += "\"exGrade\":\"" + exGrade + "\",";
                    JSON += "\"level\":\"" + level + "\",";
                    JSON += "\"incJump\":\"" + incJump + "\",";
                    JSON += "\"bossReward\":\"" + bossReward + "\",";
                    JSON += "\"sharableOnce\":\"" + sharableOnce + "\",";
                    JSON += "\"accountSharable\":\"" + accountSharable + "\",";
                    JSON += "\"baseLevel\":\"" + baseLevel + "\",";
                    JSON += "\"abilityTimeLimited\":\"" + abilityTimeLimited + "\",";
                    JSON += "\"incMHPr\":\"" + incMHPr + "\",";
                    JSON += "\"incMMPr\":\"" + incMMPr + "\",";
                    JSON += "\"equipTradeBlock\":\"" + equipTradeBlock + "\",";
                    JSON += "\"bonusExp\":\"" + bonusExp + "\",";
                    JSON += "\"reduceReq\":\"" + reduceReq + "\",";
                    JSON += "\"incCraft\":\"" + incCraft + "\",";
                    JSON += "\"scope\":\"" + scope + "\",";
                    JSON += "\"accountShareTag\":\"" + accountShareTag + "\",";
                    JSON += "\"bdR\":\"" + bdR + "\",";
                    JSON += "\"imdR\":\"" + imdR + "\",";
                    JSON += "\"noPotential\":\"" + noPotential + "\",";
                    JSON += "\"dayOfWeekItemStat\":\"" + dayOfWeekItemStat + "\",";
                    JSON += "\"slotMax\":\"" + slotMax + "\",";
                    JSON += "\"noExpend\":\"" + noExpend + "\",";
                    JSON += "\"specialID\":\"" + specialID + "\",";
                    JSON += "\"invisibleFace\":\"" + invisibleFace + "\",";
                    JSON += "\"noMoveToLocker\":\"" + noMoveToLocker + "\",";
                    JSON += "\"cashForceCharmExp\":\"" + cashForceCharmExp + "\",";
                    JSON += "\"reqPOP\":\"" + reqPOP + "\",";
                    JSON += "\"attackSpeed\":\"" + attackSpeed + "\",";
                    JSON += "\"TimeLimited\":\"" + TimeLimited + "\",";
                    JSON += "\"reqSpecJob\":\"" + reqSpecJob + "\",";
                    JSON += "\"durability\":\"" + durability + "\",";
                    JSON += "\"replace\":\"" + replace + "\",";
                    JSON += "\"insightEXP\":\"" + insightEXP + "\",";
                    JSON += "\"onlyUpgrade\":\"" + onlyUpgrade + "\",";
                    JSON += "\"epicItem\":\"" + epicItem + "\",";
                    JSON += "\"exceptUpgrade\":\"" + exceptUpgrade + "\",";
                    JSON += "\"exceptToadsHammer\":\"" + exceptToadsHammer + "\",";
                    JSON += "\"exceptTransmission\":\"" + exceptTransmission + "\",";
                    JSON += "\"equipDrop\":\"" + equipDrop + "\",";
                    JSON += "\"blockGoldHammer\":\"" + blockGoldHammer + "\",";
                    JSON += "\"bdr\":\"" + bdr + "\",";
                    JSON += "\"senseEXP\":\"" + senseEXP + "\",";
                    JSON += "\"craftEXP\":\"" + craftEXP + "\",";
                    JSON += "\"StarPlanet\":\"" + StarPlanet + "\",";
                    JSON += "\"jewelCraft\":\"" + jewelCraft + "\",";
                    JSON += "\"effect\":\"" + effect + "\",";
                    JSON += "\"tradBlock\":\"" + tradBlock + "\",";
                    JSON += "\"MaxHP\":\"" + MaxHP + "\",";
                    JSON += "\"PotionDiscount\":\"" + PotionDiscount + "\",";
                    JSON += "\"cubeExBaseOptionLevel\":\"" + cubeExBaseOptionLevel + "\",";
                    JSON += "\"bonusDrop\":\"" + bonusDrop + "\",";
                    JSON += "\"incCriticalMAXDamage\":\"" + incCriticalMAXDamage + "\",";
                    JSON += "\"expireOnLogout\":\"" + expireOnLogout + "\",";
                    JSON += "\"jokerToSetItem\":\"" + jokerToSetItem + "\",";
                    JSON += "\"speed\":\"" + speed + "\",";
                    JSON += "\"dropBlock\":\"" + dropBlock + "\",";
                    JSON += "\"incPVPDamage\":\"" + incPVPDamage + "\",";
                    JSON += "\"randVariation\":\"" + randVariation + "\",";
                    JSON += "\"incPDDr\":\"" + incPDDr + "\",";
                    JSON += "\"incMDDr\":\"" + incMDDr + "\",";
                    JSON += "\"incDAMr\":\"" + incDAMr + "\",";
                    JSON += "\"night\":\"" + night + "\",";
                    JSON += "\"CuttableCount\":\"" + CuttableCount + "\",";
                    JSON += "\"equippedEmotion\":\"" + equippedEmotion + "\",";
                    JSON += "\"equippedSound\":\"" + equippedSound + "\",";
                    JSON += "\"PDD\":\"" + PDD + "\",";
                    JSON += "\"noDrop\":\"" + noDrop + "\",";
                    JSON += "\"incCr\":\"" + incCr + "\",";
                    JSON += "\"android\":\"" + android + "\",";
                    JSON += "\"grade\":\"" + grade + "\",";
                    JSON += "\"androidKey\":\"" + androidKey + "\",";
                    JSON += "\"mulVestigeCount\":\"" + mulVestigeCount + "\",";
                    JSON += "\"incARC\":\"" + incARC + "\",";
                    JSON += "\"scanTradeBlock\":\"" + scanTradeBlock + "\",";
                    JSON += "\"reqQuestOnProgress\":\"" + reqQuestOnProgress + "\",";
                    JSON += "\"bitsSlot\":\"" + bitsSlot + "\",";
                    JSON += "\"royalSpecial\":\"" + royalSpecial + "\",";
                    JSON += "\"effectItemID\":\"" + effectItemID + "\",";
                    JSON += "\"reqJob2\":\"" + reqJob2 + "\",";
                    JSON += "\"origin\":\"" + origin + "\",";
                    JSON += "\"quest\":\"" + quest + "\",";
                    JSON += "\"enchantCategory\":\"" + enchantCategory + "\",";
                    JSON += "\"IUCMax\":\"" + IUCMax + "\",";
                    JSON += "\"transform\":\"" + transform + "\",";
                    JSON += "\"weekly\":\"" + weekly + "\",";
                    JSON += "\"masterSpecial\":\"" + masterSpecial + "\",";
                    JSON += "\"keywordEffect\":\"" + keywordEffect + "\",";
                    JSON += "\"extendFrame\":\"" + extendFrame + "\",";
                    JSON += "\"vehicleDefaultFrame\":\"" + vehicleDefaultFrame + "\",";
                    JSON += "\"cashTradeBlock\":\"" + cashTradeBlock + "\",";
                    JSON += "\"onlyCash\":\"" + onlyCash + "\",";
                    JSON += "\"sample\":\"" + sample + "\",";
                    JSON += "\"lookChangeType\":\"" + lookChangeType + "\",";
                    JSON += "\"isAbleToTradeOnce\":\"" + isAbleToTradeOnce + "\",";
                    JSON += "\"incHP\":\"" + incHP + "\",";
                    JSON += "\"acc\":\"" + acc + "\",";
                    JSON += "\"pachinko\":\"" + pachinko + "\",";
                    JSON += "\"noExtend\":\"" + noExtend + "\",";
                    JSON += "\"addtion\":\"" + addtion + "\",";
                    JSON += "\"groupEffectID\":\"" + groupEffectID + "\",";
                    JSON += "\"variableStat\":\"" + variableStat + "\",";
                    JSON += "\"undecomposable\":\"" + undecomposable + "\",";
                    JSON += "\"despair\":\"" + despair + "\",";
                    JSON += "\"love\":\"" + love + "\",";
                    JSON += "\"shine\":\"" + shine + "\",";
                    JSON += "\"blaze\":\"" + blaze + "\",";
                    JSON += "\"hum\":\"" + hum + "\",";
                    JSON += "\"bowing\":\"" + bowing + "\",";
                    JSON += "\"hot\":\"" + hot + "\",";
                    JSON += "\"range\":\"" + range + "\",";
                    JSON += "\"skill\":\"" + skill + "\",";
                    JSON += "\"pad\":\"" + pad + "\",";
                    JSON += "\"FAttribute\":\"" + FAttribute + "\",";
                    JSON += "\"FCategory\":\"" + FCategory + "\",";
                    JSON += "\"MobID\":\"" + MobID + "\",";
                    JSON += "\"mob\":\"" + mob + "\",";
                    JSON += "\"noPotentialFieldtype\":\"" + noPotentialFieldtype + "\",";
                    JSON += "\"incLUk\":\"" + incLUk + "\",";
                    JSON += "\"recovery\":\"" + recovery + "\",";
                    JSON += "\"linkedPairItem\":\"" + linkedPairItem + "\",";
                    JSON += "\"pickUpBlock\":\"" + pickUpBlock + "\",";
                    JSON += "\"pickupMeso\":\"" + pickupMeso + "\",";
                    JSON += "\"pickupItem\":\"" + pickupItem + "\",";
                    JSON += "\"pickupOthers\":\"" + pickupOthers + "\",";
                    JSON += "\"sweepForDrop\":\"" + sweepForDrop + "\",";
                    JSON += "\"longRange\":\"" + longRange + "\",";
                    JSON += "\"consumeMP\":\"" + consumeMP + "\",";
                    JSON += "\"ignorePickup\":\"" + ignorePickup + "\",";
                    JSON += "\"autoBuff\":\"" + autoBuff + "\",";
                    JSON += "\"consumeHP\":\"" + consumeHP + "\",";
                    JSON += "\"text\":\"" + text + "\",";
                    JSON += "\"textColor\":\"" + textColor + "\",";
                    JSON += "\"textOffsetX\":\"" + textOffsetX + "\",";
                    JSON += "\"textOffsetY\":\"" + textOffsetY + "\",";
                    JSON += "\"textFontSize\":\"" + textFontSize + "\",";
                    JSON += "\"textAreaX\":\"" + textAreaX + "\",";
                    JSON += "\"textAreaY\":\"" + textAreaY + "\",";
                    JSON += "\"consumeCure\":\"" + consumeCure + "\",";
                    JSON += "\"smartPet\":\"" + smartPet + "\",";
                    JSON += "\"ringOptionSkill\":\"" + ringOptionSkill + "\",";
                    JSON += "\"ringOptionSkillLv\":\"" + ringOptionSkillLv + "\",";
                    JSON += "\"chatBalloon\":\"" + chatBalloon + "\",";
                    JSON += "\"nameTag\":\"" + nameTag + "\",";
                    JSON += "\"tradeblock\":\"" + tradeblock + "\",";
                    JSON += "\"TradeBlock\":\"" + TradeBlock + "\",";
                    JSON += "\"expBuff\":\"" + expBuff + "\",";
                    JSON += "\"expRate\":\"" + expRate + "\",";
                    JSON += "\"reqRace\":\"" + reqRace + "\",";
                    JSON += "\"bloodAllianceExpRate\":\"" + bloodAllianceExpRate + "\",";
                    JSON += "\"bloodAlliancePartyExpRate\":\"" + bloodAlliancePartyExpRate + "\",";
                    JSON += "\"unchangeable\":\"" + unchangeable + "\",";
                    JSON += "\"pmdR\":\"" + pmdR + "\",";
                    JSON += "\"hitDamRatePlus\":\"" + hitDamRatePlus + "\",";
                    JSON += "\"fs\":\"" + fs + "\",";
                    JSON += "\"tamingMob\":\"" + tamingMob + "\",";
                    JSON += "\"vehicleSkillIsTown\":\"" + vehicleSkillIsTown + "\",";
                    JSON += "\"vehicleDoubleJumpLevel\":\"" + vehicleDoubleJumpLevel + "\",";
                    JSON += "\"incSwim\":\"" + incSwim + "\",";
                    JSON += "\"incFatigue\":\"" + incFatigue + "\",";
                    JSON += "\"hpRecovery\":\"" + hpRecovery + "\",";
                    JSON += "\"mpRecovery\":\"" + mpRecovery + "\",";
                    JSON += "\"vehicleNaviFlyingLevel\":\"" + vehicleNaviFlyingLevel + "\",";
                    JSON += "\"removeBody\":\"" + removeBody + "\",";
                    JSON += "\"vehicleGlideLevel\":\"" + vehicleGlideLevel + "\",";
                    JSON += "\"vehicleNewFlyingLevel\":\"" + vehicleNewFlyingLevel + "\",";
                    JSON += "\"ActionEffect\":\"" + ActionEffect + "\",";
                    JSON += "\"dx\":\"" + dx + "\",";
                    JSON += "\"dy\":\"" + dy + "\",";
                    JSON += "\"partsQuestID\":\"" + partsQuestID + "\",";
                    JSON += "\"partsCount\":\"" + partsCount + "\",";
                    JSON += "\"customVehicle\":\"" + customVehicle + "\",";
                    JSON += "\"passengerNum\":\"" + passengerNum + "\",";
                    JSON += "\"flip\":\"" + flip + "\",";
                    JSON += "\"walk\":\"" + walk + "\",";
                    JSON += "\"stand\":\"" + stand + "\",";
                    JSON += "\"attack\":\"" + attack + "\",";
                    JSON += "\"afterImage\":\"" + afterImage + "\",";
                    JSON += "\"sfx\":\"" + sfx + "\",";
                    JSON += "\"head\":\"" + head + "\",";
                    JSON += "\"knockback\":\"" + knockback + "\",";
                    JSON += "\"damR\":\"" + damR + "\",";
                    JSON += "\"icnSTR\":\"" + icnSTR + "\",";
                    JSON += "\"cantRepair\":\"" + cantRepair + "\",";
                    JSON += "\"gatherTool\":\"" + gatherTool + "\",";
                    JSON += "\"additon\":\"" + additon + "\",";
                    JSON += "\"elemDefault\":\"" + elemDefault + "\",";
                    JSON += "\"incRMAI\":\"" + incRMAI + "\",";
                    JSON += "\"incRMAL\":\"" + incRMAL + "\",";
                    JSON += "\"incRMAF\":\"" + incRMAF + "\",";
                    JSON += "\"incRMAS\":\"" + incRMAS + "\",";
                    JSON += "\"epic\":\"" + epic + "\",";
                    JSON += "\"hide\":\"" + hide + "\",";
                    JSON += "\"kaiserOffsetX\":\"" + kaiserOffsetX + "\",";
                    JSON += "\"kaiserOffsetY\":\"" + kaiserOffsetY + "\",";
                    JSON += "\"inPAD\":\"" + inPAD + "\",";
                    JSON += "\"incLuk\":\"" + incLuk + "\",";
                    JSON += "\"incAttackCount\":\"" + incAttackCount + "\",";

                    using (TextWriter tw = new StreamWriter(File.Create(path + int.Parse(item.name.Replace(".img", "")) + ".json")))
                    {
                        tw.Write(JSON.Substring(0, JSON.Length - 1) + "}");
                    }

                    //Generating list of possible values.
                    foreach (WzImageProperty prop in info.properties)
                    {

                        try
                        {
                            props.Add(prop.Name);
                        }
                        catch (Exception ex)
                        {
                            //Canvas properties fail to cast
                        }
                    }
                }
            }

            String ret = "";
            foreach (string prop in props)
            {
                ret += prop + "\r\n";
            }

            using (TextWriter tw = new StreamWriter(File.Create(path + "props.txt")))
            {
                tw.Write(ret);
            }
        }

        private void ItemStringsToJSON(WzImage cat, string path)
        {
            string list = "[";
            foreach (WzSubProperty item in cat.WzProperties)
            {
                String JSON = "{";
                string name = FixStringTabs(Regex.Replace(System.Security.SecurityElement.Escape(item["name"].ReadString("")), @"\r\n?|\n", " "));
                JSON += "\"id\": " + int.Parse(item.name) + ",";
                JSON += "\"type\": \"" + cat.name.Replace(".img", "") + "\",";
                JSON += "\"name\": \"" + name + "\",";
                JSON += "\"desc\": \"" + FixStringTabs(Regex.Replace(System.Security.SecurityElement.Escape(item["desc"].ReadString("")), @"\r\n?|\n", " ")) + "\"}";
                using (TextWriter tw = new StreamWriter(File.Create(path + @"\item\" + int.Parse(item.name) + ".json")))
                {
                    tw.Write(JSON);
                }
                list += "{\"id\":" + int.Parse(item.name) + ",\"name\":\"" + name + "\"},";
            }
            list = list.Substring(0, list.Length - 1) + "]";
            using (TextWriter tw = new StreamWriter(File.Create(path + @"\item\" + cat.name.Replace(".img", "") + ".json")))
            {
                tw.Write(list);
            }
        }

        private void StringToJSON(WzDirectory dir, string path)
        {
            //Items
            WzImage Cash = (WzImage)dir["Cash.img"];
            WzImage Consume = (WzImage)dir["Consume.img"];
            WzImage Ins = (WzImage)dir["Ins.img"];
            WzImage Pet = (WzImage)dir["Pet.img"];
            WzImage Etc = (WzImage)dir["Etc.img"];
            WzImage Eqp = (WzImage)dir["Eqp.img"];

            if (!Directory.Exists(path + @"\item\"))
            {
                Directory.CreateDirectory(path + @"\item\");
            }

            ItemStringsToJSON(Cash, path);
            ItemStringsToJSON(Consume, path);
            ItemStringsToJSON(Ins, path);
            ItemStringsToJSON(Pet, path);

            //Etc
            string etclist = "[";
            foreach (WzSubProperty item in Etc["Etc"].WzProperties)
            {
                String JSON = "{";
                string name = FixStringTabs(Regex.Replace(System.Security.SecurityElement.Escape(item["name"].ReadString("")), @"\r\n?|\n", " "));
                JSON += "\"id\": " + int.Parse(item.name) + ",";
                JSON += "\"type\": \"Equip\",";
                JSON += "\"name\": \"" + name + "\",";
                JSON += "\"desc\": \"" + FixStringTabs(Regex.Replace(System.Security.SecurityElement.Escape(item["desc"].ReadString("")), @"\r\n?|\n", " ")) + "\"}";
                etclist += "{\"id\":" + int.Parse(item.name) + ",\"name\":\"" + name + "\"},";
                using (TextWriter tw = new StreamWriter(File.Create(path + @"\item\" + int.Parse(item.name) + ".json")))
                {
                    tw.Write(JSON);
                }
            }
            etclist = etclist.Substring(0, etclist.Length - 1) + "]";
            using (TextWriter tw = new StreamWriter(File.Create(path + @"\item\Etc.json")))
            {
                tw.Write(etclist);
            }

            //Eqp
            string eqlist = "[";
            foreach (WzSubProperty cat in Eqp["Eqp"].WzProperties)
            {
                foreach (WzSubProperty item in cat.WzProperties)
                {
                    String JSON = "{";
                    string name = FixStringTabs(Regex.Replace(System.Security.SecurityElement.Escape(item["name"].ReadString("")), @"\r\n?|\n", " "));
                    JSON += "\"id\": " + int.Parse(item.name) + ",";
                    JSON += "\"type\": \"Equip\",";
                    JSON += "\"category\": \"" + cat.name + "\",";
                    JSON += "\"name\": \"" + name + "\",";
                    JSON += "\"desc\": \"" + FixStringTabs(Regex.Replace(System.Security.SecurityElement.Escape(item["desc"].ReadString("")), @"\r\n?|\n", " ")) + "\"}";
                    eqlist += "{\"id\":" + int.Parse(item.name) + ",\"name\":\"" + name + "\"},";
                    using (TextWriter tw = new StreamWriter(File.Create(path + @"\item\" + int.Parse(item.name) + ".json")))
                    {
                        tw.Write(JSON);
                    }
                }
            }
            eqlist = eqlist.Substring(0, eqlist.Length - 1) + "]";
            using (TextWriter tw = new StreamWriter(File.Create(path + @"\item\Eqp.json")))
            {
                tw.Write(eqlist);
            }

            if (!Directory.Exists(path + @"\npc\"))
            {
                Directory.CreateDirectory(path + @"\npc\");
            }

            //Npc
            string npclist = "[";
            WzImage Npc = (WzImage)dir["Npc.img"];
            foreach (WzSubProperty npc in Npc.WzProperties)
            {
                String JSON = "{";
                string name = FixStringTabs(Regex.Replace(System.Security.SecurityElement.Escape(npc["name"].ReadString("")), @"\r\n?|\n", " "));
                JSON += "\"id\": " + int.Parse(npc.name) + ",";
                JSON += "\"name\": \"" + name + "\"}";
                npclist += "{\"id\":" + int.Parse(npc.name) + ",\"name\":\"" + name + "\"},";
                using (TextWriter tw = new StreamWriter(File.Create(path + @"\npc\" + int.Parse(npc.name) + ".json")))
                {
                    tw.Write(JSON);
                }
            }
            npclist = npclist.Substring(0, npclist.Length - 1) + "]";
            using (TextWriter tw = new StreamWriter(File.Create(path + @"\npc\Npc.json")))
            {
                tw.Write(npclist);
            }

            if (!Directory.Exists(path + @"\mob\"))
            {
                Directory.CreateDirectory(path + @"\mob\");
            }

            //Mob
            string moblist = "[";
            WzImage Mob = (WzImage)dir["Mob.img"];
            foreach (WzSubProperty mob in Mob.WzProperties)
            {
                String JSON = "{";
                string name = FixStringTabs(Regex.Replace(System.Security.SecurityElement.Escape(mob["name"].ReadString("")), @"\r\n?|\n", " "));
                JSON += "\"id\": " + int.Parse(mob.name) + ",";
                JSON += "\"name\": \"" + name + "\"}";
                moblist += "{\"id\":" + int.Parse(mob.name) + ",\"name\":\"" + name + "\"},";
                using (TextWriter tw = new StreamWriter(File.Create(path + @"\mob\" + int.Parse(mob.name) + ".json")))
                {
                    tw.Write(JSON);
                }
            }
            moblist = moblist.Substring(0, moblist.Length - 1) + "]";
            using (TextWriter tw = new StreamWriter(File.Create(path + @"\mob\Mob.json")))
            {
                tw.Write(moblist);
            }

            if (!Directory.Exists(path + @"\map\"))
            {
                Directory.CreateDirectory(path + @"\map\");
            }

            //Map
            string maplist = "[";
            WzImage Map = (WzImage)dir["Map.img"];
            foreach (WzSubProperty cat in Map.WzProperties)
            {
                foreach (WzSubProperty map in cat.WzProperties)
                {
                    String JSON = "{";
                    string name = FixStringTabs(Regex.Replace(System.Security.SecurityElement.Escape(map["mapName"].ReadString("")), @"\r\n?|\n", " "));
                    JSON += "\"id\": " + int.Parse(map.name) + ",";
                    JSON += "\"region\": \"" + cat.name + "\",";
                    JSON += "\"name\": \"" + name + "\",";
                    JSON += "\"street\": \"" + FixStringTabs(Regex.Replace(System.Security.SecurityElement.Escape(map["streetName"].ReadString("")), @"\r\n?|\n", " ")) + "\"}";
                    maplist += "{\"id\":" + int.Parse(map.name) + ",\"name\":\"" + name + "\"},";
                    using (TextWriter tw = new StreamWriter(File.Create(path + @"\map\" + int.Parse(map.name) + ".json")))
                    {
                        tw.Write(JSON);
                    }
                }
            }
            maplist = maplist.Substring(0, maplist.Length - 1) + "]";
            using (TextWriter tw = new StreamWriter(File.Create(path + @"\map\Map.json")))
            {
                tw.Write(maplist);
            }

            if (!Directory.Exists(path + @"\skill\"))
            {
                Directory.CreateDirectory(path + @"\skill\");
            }

            //Skill
            string skilllist = "[";
            WzImage Skill = (WzImage)dir["Skill.img"];
            foreach (WzSubProperty skill in Skill.WzProperties)
            {
                String JSON = "{";
                string name = FixStringTabs(Regex.Replace(System.Security.SecurityElement.Escape(skill["name"].ReadString("")), @"\r\n?|\n", " "));
                if (skill["bookName"] != null) continue;
                JSON += "\"id\": " + int.Parse(skill.name) + ",";
                JSON += "\"name\": \"" + name + "\",";
                JSON += "\"desc\": \"" + FixStringTabs(Regex.Replace(System.Security.SecurityElement.Escape(skill["desc"].ReadString("")), @"\r\n?|\n", " ")) + "\"},";
                skilllist += "{\"id\":" + int.Parse(skill.name) + ",\"name\":\"" + name + "\"},";
                using (TextWriter tw = new StreamWriter(File.Create(path + @"\skill\" + int.Parse(skill.name) + ".json")))
                {
                    tw.Write(JSON);
                }
            }
            skilllist = skilllist.Substring(0, skilllist.Length - 1) + "]";
            using (TextWriter tw = new StreamWriter(File.Create(path + @"\skill\Skill.json")))
            {
                tw.Write(skilllist);
            }
        }

        private string FixStringTabs(string str)
        {
            string line = str.Replace("\t", " ");
            while (line.IndexOf("  ") >= 0)
            {
                line = line.Replace("  ", " ");
            }
            return line;
        }

        private void ItemToJSON(WzDirectory dir, string path)
        {
            WzDirectory Cash = (WzDirectory)dir["Cash"];
            WzDirectory Consume = (WzDirectory)dir["Consume"];
            WzDirectory Etc = (WzDirectory)dir["Etc"];
            WzDirectory Install = (WzDirectory)dir["Install"];
            WzDirectory Pet = (WzDirectory)dir["Pet"];
            WzDirectory Special = (WzDirectory)dir["Cash"];

            ItemCategoryToJSON(Cash, path);
            ItemCategoryToJSON(Consume, path);
            ItemCategoryToJSON(Etc, path);
            ItemCategoryToJSON(Install, path);
            ItemCategoryToJSON(Pet, path);
            ItemCategoryToJSON(Special, path);
        }

        private void ItemCategoryToJSON(WzDirectory type, string path)
        {
            try
            {
                foreach (WzImage itemCat in type.WzImages)
                {
                    foreach (WzSubProperty item in itemCat.WzProperties)
                    {
                        WzSubProperty info = (WzSubProperty)item["info"];
                        WzSubProperty spec = (WzSubProperty)item["spec"];

                        if (info == null)
                        {
                            continue;
                        }

                        using (TextWriter tw = new StreamWriter(File.Create(path + int.Parse(item.name) + ".json")))
                        {
                            //Get Icon
                            MemoryStream stream = new MemoryStream();
                            if (info["icon"] != null)
                            {
                                if (info["icon"].PropertyType == WzPropertyType.Canvas)
                                {
                                    WzCanvasProperty img = (WzCanvasProperty)info["icon"];
                                    if (img == null) continue;
                                    img.PngProperty.GetPNG(false).Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                                }
                                else if (info["icon"].PropertyType == WzPropertyType.UOL)
                                {
                                    WzUOLProperty uol = (WzUOLProperty)info["icon"];
                                    WzCanvasProperty img = (WzCanvasProperty)uol.LinkValue;
                                    if (img == null) continue;
                                    img.PngProperty.GetPNG(false).Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                                }
                            }

                            byte[] pngbytes = stream.ToArray();
                            stream.Close();

                            //Get price
                            int price = info["price"] == null ? 0 : info["price"].ReadValue();

                            //Get SlotMax
                            int slotMax = info["slotMax"] == null ? 1 : info["slotMax"].ReadValue();

                            //Get cash
                            int isCash = info["cash"] == null ? 0 : info["cash"].ReadValue();

                            //Get tradeblock
                            int tradeBlock = info["tradeBlock"] == null ? 0 : info["tradeBlock"].ReadValue();

                            string JSON = "";
                            JSON += "{";
                            JSON += "\"price\":" + price + ",";
                            JSON += "\"slotMax\":" + slotMax + ",";
                            JSON += "\"cash\":" + isCash + ",";
                            JSON += "\"tradeBlock\":" + tradeBlock + ",";
                            JSON += "\"icon\":\"" + Convert.ToBase64String(pngbytes) + "\",";
                            JSON += "\"spec\":[";

                            if (spec != null)
                            {
                                try
                                {
                                    foreach (WzSubProperty effect in spec.WzProperties)
                                    {
                                        if (effect.PropertyType == WzPropertyType.String)
                                        {
                                            JSON += "{\"name\":\"" + effect.name + "\", \"value\":\"" + effect.ReadString("") + "\"},";
                                        }
                                        else
                                        {
                                            JSON += "{\"name\":\"" + effect.name + "\", \"value\":" + effect.ReadValue() + "},";
                                        }
                                    }
                                    JSON = JSON.Substring(0, JSON.Length - 1);
                                }
                                catch (Exception ex)
                                {

                                }
                            }
                            JSON += "]}";
                            tw.Write(JSON);
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error: " + type.name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }



    public class WzClassicXmlSerializer : WzXmlSerializer, IWzImageSerializer
    {
        public WzClassicXmlSerializer(int indentation, LineBreak lineBreakType, bool exportbase64)
            : base(indentation, lineBreakType)
        { ExportBase64Data = exportbase64; }

        private void exportXmlInternal(WzImage img, string path)
        {
            bool parsed = img.Parsed || img.Changed;
            if (!parsed)
                img.ParseImage();
            curr++;


            using (TextWriter tw = new StreamWriter(File.Create(path)))
            {
                tw.Write("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" + lineBreak);
                tw.Write("<imgdir name=\"" + XmlUtil.SanitizeText(img.Name) + "\">" + lineBreak);
                foreach (WzImageProperty property in img.WzProperties)
                    WritePropertyToXML(tw, indent, property);
                tw.Write("</imgdir>" + lineBreak);
            }

            if (!parsed)
                img.UnparseImage();
        }

        private void exportDirXmlInternal(WzDirectory dir, string path)
        {
            if (!Directory.Exists(path))
                createDirSafe(ref path);

            if (path.Substring(path.Length - 1) != @"\")
                path += @"\";

            foreach (WzDirectory subdir in dir.WzDirectories)
            {
                exportDirXmlInternal(subdir, path + subdir.name + @"\");
            }
            foreach (WzImage subimg in dir.WzImages)
            {
                exportXmlInternal(subimg, path + subimg.Name + ".xml");
            }
        }

        public void SerializeImage(WzImage img, string path)
        {
            total = 1; curr = 0;
            if (Path.GetExtension(path) != ".xml") path += ".xml";
            exportXmlInternal(img, path);
        }

        public void SerializeDirectory(WzDirectory dir, string path)
        {
            total = dir.CountImages(); curr = 0;
            exportDirXmlInternal(dir, path);
        }

        public void SerializeFile(WzFile file, string path)
        {
            SerializeDirectory(file.WzDirectory, path);
        }
    }

    public class WzNewXmlSerializer : WzXmlSerializer
    {
        public WzNewXmlSerializer(int indentation, LineBreak lineBreakType)
            : base(indentation, lineBreakType)
        { }

        internal void DumpImageToXML(TextWriter tw, string depth, WzImage img)
        {
            bool parsed = img.Parsed || img.Changed;
            if (!parsed) img.ParseImage();
            curr++;
            tw.Write(depth + "<wzimg name=\"" + XmlUtil.SanitizeText(img.Name) + "\">" + lineBreak);
            string newDepth = depth + indent;
            foreach (WzImageProperty property in img.WzProperties)
                WritePropertyToXML(tw, newDepth, property);
            tw.Write(depth + "</wzimg>");
            if (!parsed) img.UnparseImage();
        }

        internal void DumpDirectoryToXML(TextWriter tw, string depth, WzDirectory dir)
        {
            tw.Write(depth + "<wzdir name=\"" + XmlUtil.SanitizeText(dir.Name) + "\">" + lineBreak);
            foreach (WzDirectory subdir in dir.WzDirectories)
                DumpDirectoryToXML(tw, depth + indent, subdir);
            foreach (WzImage img in dir.WzImages)
                DumpImageToXML(tw, depth + indent, img);
            tw.Write(depth + "</wzdir>" + lineBreak);
        }

        public void ExportCombinedXml(List<WzObject> objects, string path)
        {
            total = 1; curr = 0;
            if (Path.GetExtension(path) != ".xml") path += ".xml";
            foreach (WzObject obj in objects)
            {
                if (obj is WzImage) total++;
                else if (obj is WzDirectory) total += ((WzDirectory)obj).CountImages();
            }

            ExportBase64Data = true;
            TextWriter tw = new StreamWriter(path);
            tw.Write("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" + lineBreak);
            tw.Write("<xmldump>" + lineBreak);
            foreach (WzObject obj in objects)
            {
                if (obj is WzDirectory) DumpDirectoryToXML(tw, indent, (WzDirectory)obj);
                else if (obj is WzImage) DumpImageToXML(tw, indent, (WzImage)obj);
                else if (obj is WzImageProperty) WritePropertyToXML(tw, indent, (WzImageProperty)obj);
            }
            tw.Write("</xmldump>" + lineBreak);
            tw.Close();
        }
    }

    public class WzXmlDeserializer : ProgressingWzSerializer
    {
        public static NumberFormatInfo formattingInfo;

        private bool useMemorySaving;
        private byte[] iv;
        private WzImgDeserializer imgDeserializer = new WzImgDeserializer(false);

        public WzXmlDeserializer(bool useMemorySaving, byte[] iv)
            : base()
        {
            this.useMemorySaving = useMemorySaving;
            this.iv = iv;
        }

        #region Public Functions
        public List<WzObject> ParseXML(string path)
        {
            List<WzObject> result = new List<WzObject>();
            XmlDocument doc = new XmlDocument();
            doc.Load(path);
            XmlElement mainElement = (XmlElement)doc.ChildNodes[1];
            curr = 0;
            if (mainElement.Name == "xmldump")
            {
                total = CountImgs(mainElement);
                foreach (XmlElement subelement in mainElement)
                {
                    if (subelement.Name == "wzdir")
                        result.Add(ParseXMLWzDir(subelement));
                    else if (subelement.Name == "wzimg")
                        result.Add(ParseXMLWzImg(subelement));
                    else throw new InvalidDataException("unknown XML prop " + subelement.Name);
                }
            }
            else if (mainElement.Name == "imgdir")
            {
                total = 1;
                result.Add(ParseXMLWzImg(mainElement));
                curr++;
            }
            else throw new InvalidDataException("unknown main XML prop " + mainElement.Name);
            return result;
        }
        #endregion

        #region Internal Functions
        internal int CountImgs(XmlElement element)
        {
            int result = 0;
            foreach (XmlElement subelement in element)
            {
                if (subelement.Name == "wzimg") result++;
                else if (subelement.Name == "wzdir") result += CountImgs(subelement);
            }
            return result;
        }

        internal WzDirectory ParseXMLWzDir(XmlElement dirElement)
        {
            WzDirectory result = new WzDirectory(dirElement.GetAttribute("name"));
            foreach (XmlElement subelement in dirElement)
            {
                if (subelement.Name == "wzdir")
                    result.AddDirectory(ParseXMLWzDir(subelement));
                else if (subelement.Name == "wzimg")
                    result.AddImage(ParseXMLWzImg(subelement));
                else throw new InvalidDataException("unknown XML prop " + subelement.Name);
            }
            return result;
        }

        internal WzImage ParseXMLWzImg(XmlElement imgElement)
        {
            string name = imgElement.GetAttribute("name");
            WzImage result = new WzImage(name);
            foreach (XmlElement subelement in imgElement)
                result.WzProperties.Add(ParsePropertyFromXMLElement(subelement));
            result.Changed = true;
            if (this.useMemorySaving)
            {
                string path = Path.GetTempFileName();
                WzBinaryWriter wzWriter = new WzBinaryWriter(File.Create(path), iv);
                result.SaveImage(wzWriter);
                wzWriter.Close();
                result.Dispose();

                bool successfullyParsedImage;
                result = imgDeserializer.WzImageFromIMGFile(path, iv, name, out successfullyParsedImage);
            }
            return result;
        }

        internal WzImageProperty ParsePropertyFromXMLElement(XmlElement element)
        {
            switch (element.Name)
            {
                case "imgdir":
                    WzSubProperty sub = new WzSubProperty(element.GetAttribute("name"));
                    foreach (XmlElement subelement in element)
                        sub.AddProperty(ParsePropertyFromXMLElement(subelement));
                    return sub;

                case "canvas":
                    WzCanvasProperty canvas = new WzCanvasProperty(element.GetAttribute("name"));
                    if (!element.HasAttribute("basedata")) throw new NoBase64DataException("no base64 data in canvas element with name " + canvas.Name);
                    canvas.PngProperty = new WzPngProperty();
                    MemoryStream pngstream = new MemoryStream(Convert.FromBase64String(element.GetAttribute("basedata")));
                    canvas.PngProperty.SetPNG((Bitmap)Image.FromStream(pngstream, true, true));
                    foreach (XmlElement subelement in element)
                        canvas.AddProperty(ParsePropertyFromXMLElement(subelement));
                    return canvas;

                case "int":
                    WzIntProperty compressedInt = new WzIntProperty(element.GetAttribute("name"), int.Parse(element.GetAttribute("value"), formattingInfo));
                    return compressedInt;

                case "double":
                    WzDoubleProperty doubleProp = new WzDoubleProperty(element.GetAttribute("name"), double.Parse(element.GetAttribute("value"), formattingInfo));
                    return doubleProp;

                case "null":
                    WzNullProperty nullProp = new WzNullProperty(element.GetAttribute("name"));
                    return nullProp;

                case "sound":
                    if (!element.HasAttribute("basedata") || !element.HasAttribute("basehead") || !element.HasAttribute("length")) throw new NoBase64DataException("no base64 data in sound element with name " + element.GetAttribute("name"));
                    WzSoundProperty sound = new WzSoundProperty(element.GetAttribute("name"),
                        int.Parse(element.GetAttribute("length")),
                        Convert.FromBase64String(element.GetAttribute("basehead")),
                        Convert.FromBase64String(element.GetAttribute("basedata")));
                    return sound;

                case "string":
                    WzStringProperty stringProp = new WzStringProperty(element.GetAttribute("name"), element.GetAttribute("value"));
                    return stringProp;

                case "short":
                    WzShortProperty shortProp = new WzShortProperty(element.GetAttribute("name"), short.Parse(element.GetAttribute("value"), formattingInfo));
                    return shortProp;

                case "long":
                    WzLongProperty longProp = new WzLongProperty(element.GetAttribute("name"), long.Parse(element.GetAttribute("value"), formattingInfo));
                    return longProp;

                case "uol":
                    WzUOLProperty uol = new WzUOLProperty(element.GetAttribute("name"), element.GetAttribute("value"));
                    return uol;

                case "vector":
                    WzVectorProperty vector = new WzVectorProperty(element.GetAttribute("name"), new WzIntProperty("x", Convert.ToInt32(element.GetAttribute("x"))), new WzIntProperty("y", Convert.ToInt32(element.GetAttribute("y"))));
                    return vector;

                case "float":
                    WzFloatProperty floatProp = new WzFloatProperty(element.GetAttribute("name"), float.Parse(element.GetAttribute("value"), formattingInfo));
                    return floatProp;

                case "extended":
                    WzConvexProperty convex = new WzConvexProperty(element.GetAttribute("name"));
                    foreach (XmlElement subelement in element)
                        convex.AddProperty(ParsePropertyFromXMLElement(subelement));
                    return convex;
            }
            throw new InvalidDataException("unknown XML prop " + element.Name);
        }
        #endregion
    }
}