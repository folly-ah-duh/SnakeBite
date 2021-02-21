﻿// SYNC to makebite
#define SNAKEBITE //TODO bad
using GzsTool.Core.Common;
using GzsTool.Core.Common.Interfaces;
using GzsTool.Core.Fpk;
using GzsTool.Core.Qar;
using GzsTool.Core.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SnakeBite.GzsTool
{
    public static class GzsLib
    {
        //GAMEVERSION: qar flags, should be updated to games flags (use gzstool on unmodded .dat and check flags)
        public static uint zeroFlags = 3150304;
        public static uint oneFlags = 3150048;
        public static uint chunk0Flags = 3146208;
        public static uint chunk7Flags = 3146208;
        public static uint texture7Flags = 3150048;

        //based on tpp 1.0.12 vanilla entire file set
        private static Dictionary<string, List<string>> archiveExtensions = new Dictionary<string, List<string>> {
            {"dat",new List<string> {
                "bnk",
                "dat",
                "ffnt",
                "fmtt",
                "fpk",
                "fpkd",
                "fsm",
                "fsop",
                "ftex",
                "ftexs",
                "json",
                "lua",
                "pftxs",
                "sbp",
                "subp",
                "wem",
            }},
            {"fpk",new List<string> {
                "adm",
                "atsh",
                "caar",
                "dfrm",
                "ends",
                "fclo",
                "fcnp",
                "fdes",
                "fmdl",
                "fnt",
                "frdv",
                "frig",
                "frl",
                "frt",
                "fsm",
                "fstb",
                "ftdp",
                "ftex",
                "fv2",
                "geobv",
                "geom",
                "geoms",
                "gpfp",
                "grxla",
                "grxoc",
                "gskl",
                "htre",
                "ladb",
                "lba",
                "lng2",
                "lpsh",
                "mbl",
                "mog",
                "mtar",
                "mtard",
                "nav2",
                "nta",
                "obr",
                "obrb",
                "pcsp",
                "rdf",
                "sand",
                "sani",
                "spch",
                "subp",
                "tcvp",
                "trap",
                "tre2",
                "twpf",
                "uia",
                "uif",
                "uigb",
                "uilb",
            }},
            //tex in fpkd extension sort order (see SortFpksFiles) 
            //tex derived by an incomplete analysis of vanilla fpkds 
            //all positions reletively solid except for "bnd", analysis from my ExtensionOrder.lua puts it somewhere between veh and tgt.
            //have put it between des and tgt in line with init.lua RegisterPackageExtensionInfo call
            // RegisterPackageExtensionInfo call seems to mostly match my derived order in reverse - however clo doesnt fit the order and lng isn't in its table.
            //previously file type order wasn't actually currently being handled by snakebite. The issue has been avoided so far by new fpkds not being merged with anything and merges with game fpkds being replacements rather than additions. 
            //Actual load order of files might be reversed however, given how files in extension groups are alpha descending but (if evidence from lua load order applies to all) are loaded acending
              {"fpkd",new List<string> {
                "fox2",
                "evf",
                "parts",
                "vfxlb",
                "vfx",
                "vfxlf",
                "veh",
                "frld",
                "des",
                "bnd",
                "tgt",
                "phsd",
                "ph",
                "sim",
                "clo",
                "fsd",
                "sdf",
                "lua",
                "lng",
            }},
        };

        static Dictionary<string, string> extensionToType = new Dictionary<string, string> {
            {"dat", "QarFile"},
            {"fpk", "FpkFile" },
            {"fpkd", "FpkFile" },
        };

        // Extract full archive
        public static List<string> ExtractArchive<T>(string FileName, string OutputPath) where T : ArchiveFile, new()
        {
            if (!File.Exists(FileName))
            {
                Debug.LogLine($"[GzsLib] File not found: {FileName}");
                throw new FileNotFoundException();
            }
            else
            {
                string name = Path.GetFileName(FileName);
                Debug.LogLine($"[GzsLib] Extracting {name} to {OutputPath} ({Tools.GetFileSizeKB(FileName)} KB)");

                using (FileStream archiveFile = new FileStream(FileName, FileMode.Open))
                {
                    List<string> outFiles = new List<string>();
                    T archive = new T();
                	archive.Name = Path.GetFileName(FileName);
                    archive.Read(archiveFile);

                    // Extract all files
                    var exportedFiles = archive.ExportFiles(archiveFile);
                    foreach (var v in exportedFiles)
                    {
                        string outDirectory = Path.Combine(OutputPath, Path.GetDirectoryName(v.FileName));
                        string outFileName = Path.Combine(OutputPath, v.FileName);
                        if (!Directory.Exists(outDirectory)) Directory.CreateDirectory(outDirectory);
                        using (FileStream outStream = new FileStream(outFileName, FileMode.Create))
                        {
                            // copy to output stream
                            v.DataStream().CopyTo(outStream);
                            outFiles.Add(v.FileName);
                        }
                    }
                    Debug.LogLine($"[GzsLib] Extracted {outFiles.Count} files from {name}");
                    return outFiles;
                }
            }
        }

        // Extract single file from archive
        public static bool ExtractFile<T>(string SourceArchive, string FilePath, string OutputFile) where T : ArchiveFile, new()
        {
            if (!File.Exists(SourceArchive))
            {
                Debug.LogLine($"[GzsLib] File not found: {SourceArchive}");
                throw new FileNotFoundException();
            }
            else
            {
                Debug.LogLine(String.Format("[GzsLib] Extracting file {1}: {0} -> {2}", FilePath, SourceArchive, OutputFile));
                // Get file hash from path
                ulong fileHash = Tools.NameToHash(FilePath);

                using (FileStream archiveFile = new FileStream(SourceArchive, FileMode.Open))
                {
                    T archive = new T();
                    archive.Name = Path.GetFileName(SourceArchive);
                    archive.Read(archiveFile);

                    // Select single file for output
                    var outFile = archive.ExportFiles(archiveFile).FirstOrDefault(entry => Tools.NameToHash(entry.FileName) == fileHash);

                    if (outFile != null)
                    {
                        string path = Path.GetDirectoryName(Path.GetFullPath(OutputFile));
                        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                        using (FileStream outStream = new FileStream(OutputFile, FileMode.Create))
                        {
                            // copy to output stream
                            outFile.DataStream().CopyTo(outStream);
                        }
                        return true;
                    }
                    else
                    {
                        // file not found
                        return false;
                    }
                }
            }
            
        }

        // Extract single file from archive
        public static bool ExtractFileByHash<T>(string SourceArchive, ulong FileHash, string OutputFile) where T : ArchiveFile, new()
        {
            if (!File.Exists(SourceArchive))
            {
                Debug.LogLine($"[GzsLib] File not found: {SourceArchive}");
                throw new FileNotFoundException();
            }
            else
            {
                Debug.LogLine(String.Format("[GzsLib] Extracting file from {1}: hash {0} -> {2}", FileHash, SourceArchive, OutputFile));
                // Get file hash from path
                ulong fileHash = FileHash;

                using (FileStream archiveFile = new FileStream(SourceArchive, FileMode.Open))
                {
                    T archive = new T();
                    archive.Name = Path.GetFileName(SourceArchive);
                    archive.Read(archiveFile);

                    // Select single file for output
                    var outFile = archive.ExportFiles(archiveFile).FirstOrDefault(entry => Tools.NameToHash(entry.FileName) == fileHash);

                    if (outFile != null)
                    {
                        if (!Directory.Exists(Path.GetDirectoryName(OutputFile))) Directory.CreateDirectory(Path.GetDirectoryName(OutputFile));
                        using (FileStream outStream = new FileStream(OutputFile, FileMode.Create))
                        {
                            // copy to output stream
                            outFile.DataStream().CopyTo(outStream);
                        }
                        return true;
                    }
                    else
                    {
                        // file not found
                        return false;
                    }
                }
            }
            
        }

        // Read file hashes contained within QAR archive
        public static List<GameFile> ListArchiveHashes<T>(string ArchiveName) where T : ArchiveFile, new()
        {
            if (!File.Exists(ArchiveName))
            {
                Debug.LogLine($"[GzsLib] File not found: {ArchiveName}");
                throw new FileNotFoundException();
            }
            else
            {
                string name = Path.GetFileName(ArchiveName);
                Debug.LogLine($"[GzsLib] Reading archive contents: {name} ({Tools.GetFileSizeKB(ArchiveName)} KB)");
                using (FileStream archiveFile = new FileStream(ArchiveName, FileMode.Open))
                {
                    List<GameFile> archiveContents = new List<GameFile>();
                    T archive = new T();
                	archive.Name = Path.GetFileName(ArchiveName);
                    archive.Read(archiveFile);
                    foreach (var x in archive.ExportFiles(archiveFile))
                    {
                        archiveContents.Add(new GameFile() { FilePath = x.FileName, FileHash = Tools.NameToHash(x.FileName), QarFile = archive.Name });
                    }
                    return archiveContents;
                }
            }
        }

        /// <summary>
        /// return gamefiles by hash for given qar
        /// does not include texture qars since it's currently only really used to look up fpks/archive files
        /// </summary>
        public static Dictionary<ulong, GameFile> GetQarGameFiles(string qarPath)
        {
            if (!File.Exists(qarPath))
            {
                Debug.LogLine($"[GzsLib] File not found: {qarPath}");
                throw new FileNotFoundException();
            }
            else
            {
                string name = Path.GetFileName(qarPath);
                Debug.LogLine($"[GzsLib] Reading archive contents: {name}");
                using (FileStream archiveFile = new FileStream(qarPath, FileMode.Open))
                {
                    var qarFiles = new Dictionary<ulong, GameFile>();
                    var qarFile = new QarFile();
                qarFile.Name = Path.GetFileName(qarPath);
                    qarFile.Read(archiveFile);
                    foreach (QarEntry entry in qarFile.Entries)
                    {
                        qarFiles[entry.Hash] = new GameFile() { FilePath = entry.FilePath, FileHash = entry.Hash, QarFile = qarFile.Name };
                    }
                    return qarFiles;
                }
            }
        }


        /// <summary>
        /// Returns list of files within archive
        /// </summary>
        /// <typeparam name="T">GzsTool archive type</typeparam>
        /// <param name="ArchiveName">Path to archive</param>
        /// <returns>list of files within archive</returns>
        public static List<string> ListArchiveContents<T>(string ArchiveName) where T : ArchiveFile, new()
        {
            if (!File.Exists(ArchiveName))
            {
                Debug.LogLine($"[GzsLib] File not found: {ArchiveName}");
                throw new FileNotFoundException();
            }
            else
            {
                string name = Path.GetFileName(ArchiveName);
                Debug.LogLine($"[GzsLib] Reading archive contents: {name}");
                using (FileStream archiveFile = new FileStream(ArchiveName, FileMode.Open))
                {
                    List<string> archiveContents = new List<string>();
                    T archive = new T();
                archive.Name = Path.GetFileName(ArchiveName);
                    archive.Read(archiveFile);
                    foreach (var x in archive.ExportFiles(archiveFile))
                    {
                        archiveContents.Add(x.FileName);
                    }
                    return archiveContents;
                }
            }
        }

        /// <summary>
        /// Load filename dictionaries into Hashing
        /// </summary>
        public static void LoadDictionaries()
        {
            Debug.LogLine("[GzsLib] Loading base dictionaries");
            Hashing.ReadDictionary("qar_dictionary.txt");
            Hashing.ReadMd5Dictionary("fpk_dictionary.txt");
            HashingExtended.ReadDictionary();

#if SNAKEBITE
            LoadModDictionaries();
#endif
        }

#if SNAKEBITE
        /// <summary>
        /// Adds filenames to Hashing dictionaries
        /// </summary>
        public static void LoadModDictionaries()
        {
            SettingsManager manager = new SettingsManager(GamePaths.SnakeBiteSettings);
            //fpk dictionary only really needed for gz
            //var FpkNames = manager.GetModFpkFiles();
            var QarNames = manager.GetModQarFiles(true);

            //File.WriteAllLines("mod_fpk_dict.txt", FpkNames);
            File.WriteAllLines("mod_qar_dict.txt", QarNames);

            //Hashing.ReadMd5Dictionary("mod_fpk_dict.txt");
            Hashing.ReadDictionary("mod_qar_dict.txt");
        }

        /// <summary>
        /// Adds qar filenames to Hashing dictionary for given modentry
        /// </summary>
        public static void LoadModDictionary(ModEntry modEntry)
        {
            Debug.LogLine("[GzsLib] Loading mod dictionary");

            List<string> qarNames = new List<string>();
            foreach (ModQarEntry qarFile in modEntry.ModQarEntries)
            {
                string fileName = Tools.ToQarPath(qarFile.FilePath.Substring(0, qarFile.FilePath.IndexOf(".")));
                qarNames.Add(fileName);
            }

            File.WriteAllLines("mod_qar_dict.txt", qarNames);
            Hashing.ReadDictionary("mod_qar_dict.txt");
        }

        // Gets contents of most game dats
        // Returns list (in game file priority order) of hash,GameFile dictionaries
        public static List<Dictionary<ulong, GameFile>> ReadBaseData()
        {
            Debug.LogLine("[GzsLib] Acquiring base game data");

            var baseDataFiles = new List<Dictionary<ulong, GameFile>>();
            string dataDir = Path.Combine(GamePaths.GameDir, "master");

            //in priority order SYNC with or read foxfs.dat directly
            var qarFileNames = new List<string> {
                "a_chunk7.dat",
                "data1.dat",
                "chunk0.dat",
                "chunk1.dat",
                "chunk2.dat",
                "chunk3.dat",
                "chunk4.dat",
                "chunk5_mgo0.dat",
                "chunk6_gzs0.dat",
            };

            foreach (var qarFileName in qarFileNames)
            {
                var path = Path.Combine(dataDir, qarFileName);
                if (!File.Exists(path))
                {
                    Debug.LogLine($"[GzsLib] Could not find {path}");
                } else
                {
                    var qarGameFiles = GetQarGameFiles(Path.Combine(dataDir, path));
                    baseDataFiles.Add(qarGameFiles);
                }
            }

            return baseDataFiles;
        }
#endif
        // Export FPK archive with specified parameters
        public static void WriteFpkArchive(string FileName, string SourceDirectory, List<string> Files)
        {
            Debug.LogLine(String.Format("[GzsLib] Writing FPK archive: {0}", FileName));

            string fpkType = FileName.EndsWith(".fpkd") ? "fpkd" : "fpk";
            List<string> fpkFilesSorted = SortFpksFiles(fpkType, Files);

            FpkFile q = new FpkFile() { Name = FileName, FpkType = (fpkType == "fpkd" ? FpkType.Fpkd : FpkType.Fpk) };
            foreach (string s in fpkFilesSorted)
            {
                q.Entries.Add(new FpkEntry() { FilePath = Tools.ToQarPath(s) });
            }

            using (FileStream outFile = new FileStream(FileName, FileMode.Create))
            {
                IDirectory fileDirectory = new FileSystemDirectory(SourceDirectory);
                q.Write(outFile, fileDirectory);
            }
        }

        // Export QAR archive with specified parameters
        public static void WriteQarArchive(string FileName, string SourceDirectory, List<string> Files, uint Flags)
        {
            Debug.LogLine($"[GzsLib] Writing {Path.GetFileName(FileName)}");
            List<QarEntry> qarEntries = new List<QarEntry>();
            foreach (string s in Files)
            {
                if (s.EndsWith("_unknown")) { continue; }
                qarEntries.Add(new QarEntry() { FilePath = s, Hash = Tools.NameToHash(s), Compressed = (Path.GetExtension(s).EndsWith(".fpk") || Path.GetExtension(s).EndsWith(".fpkd")) ? true : false });
            }

            QarFile q = new QarFile() { Entries = qarEntries, Flags = Flags, Name = FileName };

            using (FileStream outFile = new FileStream(FileName, FileMode.Create))
            {
                IDirectory fileDirectory = new FileSystemDirectory(SourceDirectory);
                q.Write(outFile, fileDirectory);
            }
        }

        public static void PromoteQarArchive(string sourcePath, string destinationPath)
        {
            if (File.Exists(sourcePath))
            {
                Debug.LogLine($"[GzsLib] Promoting {Path.GetFileName(sourcePath)} to {Path.GetFileName(destinationPath)} ({Tools.GetFileSizeKB(sourcePath)} KB)");
                File.Delete(destinationPath);
                File.Move(sourcePath, destinationPath);
            }
            else
            {
                Debug.LogLine($"[GzsLib] {sourcePath} not found");
            }
        }

        //SYNC: makebite
        //tex fpkds seem to require a specific order to their files.
        //Don't know whether this is also an issue for fpks, or other archives (are there any other archives with multiple filetypes?)
        //Reproduction: Extract an unmodified fpkd (such as chunk0_dat\Assets\tpp\pack\mission2\init\init.fpkd, as it's loaded automatically and early) DEBUGNOW redo this test to confirm issue again
        //change the order of the file entries in the .fpkd.xml so that they're not grouped by extension
        //repack and the load the game
        //game will fail to load
        //as the issue doesn't seem to happen when there are no fox2s in fpkd VERIFY
        //it might simply be that the first entries must be fox2s?
        //Furthermore, entries are also sorted alphanumeric descending (though not sure the exact type of alpha sort kjp used), but (lua at the very least) loaded in alpha ascending (reversed list)
        //Reproduction: have two lua files, one referencing the other (or rather a field of the other ie somescript.somevar) directly in its load time script (ie not hidden in a function) - (this also suggests that lua files are just loaded to fpkd order rather than having a load order specified anywhere) DEBUGNOW actually test lol
        //Or just hook luaL_loadbuffer and see the load order for a vanilla file.
        //GOTCHA: This mean there's a currently unresolved (unresolvable?) problem of mixing in hashed entries as you can't know their position. Though that's only an issue for GZ fpks? (and s/makebite isn't for gz)
        public static List<string> SortFpksFiles(string FpkType, List<string> fpkFiles)
        {
            List<string> fpkdExtensionsOrder = archiveExtensions[FpkType];

            List<string> fpkFilesSorted = fpkFiles.OrderBy(fileName => Path.GetExtension(fileName)).ThenByDescending(fileName => fileName).ToList();
            Dictionary<string, List<string>> filesByExtension = new Dictionary<string, List<string>>();

            if (fpkFilesSorted.Count() > 1)
            {
                if (FpkType == "fpkd")
                {
                    foreach (var fileName in fpkFilesSorted)
                    {
                        var extension = Path.GetExtension(fileName).Substring(1);
                        List<string> extensionFiles = null;
                        filesByExtension.TryGetValue(extension, out extensionFiles);
                        if (extensionFiles == null)
                        {
                            extensionFiles = new List<string>();
                            filesByExtension.Add(extension, extensionFiles);
                        }
                        extensionFiles.Add(fileName);
                    }

                    //tex sorted by alpha descending as per vanilla, GOTCHA: but not sure what acual alpha sort kjp used 
                    foreach (KeyValuePair<string, List<string>> entry in filesByExtension)
                    {
                        entry.Value.Sort((a, b) => b.CompareTo(a));
                    }

                    fpkFilesSorted = new List<string>();
                    foreach (var extension in fpkdExtensionsOrder)
                    {
                        List<string> extensionFiles = null;
                        filesByExtension.TryGetValue(extension, out extensionFiles);
                        if (extensionFiles != null)
                        {
                            foreach (var fileName in extensionFiles)
                            {
                                fpkFilesSorted.Add(fileName);
                            }
                        }
                    }
                }//if FpkType == "fpkd"
            }//if fpkFilesSorted.Count
            return fpkFilesSorted;
        }// SortFpksFiles

        public static bool IsExtensionValidForArchive(string fileName, string archiveName)
        {
            var archiveExtension = Path.GetExtension(archiveName).TrimStart('.');
            var validExtensions = archiveExtensions[archiveExtension];
            var ext = Path.GetExtension(fileName).TrimStart('.');
            bool isValid = false;
            foreach (var validExt in validExtensions)
            {
                if (ext == validExt)
                {
                    isValid = true;
                    break;
                }
            }
            if (!isValid)
            {
                return false;
            }
            return true;
        }
    }

    // Hashing snippet to check outdated filenames
    public static class HashingExtended
    {
        private static readonly Dictionary<ulong, string> HashNameDictionary = new Dictionary<ulong, string>();

        private const ulong MetaFlag = 0x4000000000000;

        public static void ReadDictionary(string path = "qar_dictionary.txt")
        {
            foreach (var line in File.ReadAllLines(path))
            {
                ulong hash = HashFileName(line) & 0x3FFFFFFFFFFFF;
                if (HashNameDictionary.ContainsKey(hash) == false)
                {
                    HashNameDictionary.Add(hash, line);
                }
            }
        }

        public static string UpdateName(string inputFile)
        {
            string filename = Path.GetFileNameWithoutExtension(inputFile);
            string ext = Path.GetExtension(inputFile);
            string extInner = "";
            if (filename.Contains(".")) // Ex: .1.ftexs, .eng.lng
            {
                extInner = Path.GetExtension(filename);
                filename = Path.GetFileNameWithoutExtension(filename);
            }

            ulong fileNameHash;
            if (TryGetFileNameHash(filename, out fileNameHash))
            {
                string foundFileNoExt;
                if (TryGetFilePathFromHash(fileNameHash, out foundFileNoExt))
                {
                    return foundFileNoExt + extInner + ext;
                }

            }

            return null;
        }

        private static ulong HashFileName(string text, bool removeExtension = true)
        {
            if (removeExtension)
            {
                int index = text.IndexOf('.');
                text = index == -1 ? text : text.Substring(0, index);
            }

            bool metaFlag = false;
            const string assetsConstant = "/Assets/";
            if (text.StartsWith(assetsConstant))
            {
                text = text.Substring(assetsConstant.Length);

                if (text.StartsWith("tpptest"))
                {
                    metaFlag = true;
                }
            }
            else
            {
                metaFlag = true;
            }

            text = text.TrimStart('/');

            const ulong seed0 = 0x9ae16a3b2f90404f;
            byte[] seed1Bytes = new byte[sizeof(ulong)];
            for (int i = text.Length - 1, j = 0; i >= 0 && j < sizeof(ulong); i--, j++)
            {
                seed1Bytes[j] = Convert.ToByte(text[i]);
            }
            ulong seed1 = BitConverter.ToUInt64(seed1Bytes, 0);
            ulong maskedHash = CityHash.CityHash.CityHash64WithSeeds(text, seed0, seed1) & 0x3FFFFFFFFFFFF;

            return metaFlag
                ? maskedHash | MetaFlag
                : maskedHash;
        }

        private static bool TryGetFilePathFromHash(ulong hash, out string filePath)
        {
            bool foundFileName = true;
            ulong pathHash = hash & 0x3FFFFFFFFFFFF;

            if (!HashNameDictionary.TryGetValue(pathHash, out filePath))
            {
                foundFileName = false;
            }

            return foundFileName;
        }

        private static bool TryGetFileNameHash(string filename, out ulong fileNameHash)
        {
            bool isConverted = true;
            try
            {
                fileNameHash = Convert.ToUInt64(filename, 16);
            }
            catch (FormatException)
            {
                isConverted = false;
                fileNameHash = 0;
            }
            return isConverted;
        }
    }
}