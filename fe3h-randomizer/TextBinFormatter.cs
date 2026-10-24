using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fire_Emblem_Three_Houses_Randomizer_V2
{
    public class TextBinFormatter : TextFormatter
    {
        uint languageCount;
        List<uint> languagePointers;
        List<uint> languageSizes;
        List<TextBinLanguageSection> languages;

        public TextBinFormatter() { }

        public TextBinFormatter(byte[] buffer)
        {
            int offset = 0;
            languageCount = BitConverter.ToUInt32(buffer, offset);
            offset += 4;

            languagePointers = new List<uint>();
            languageSizes = new List<uint>();
            languages = new List<TextBinLanguageSection>();
            for (int i = 0; i < languageCount; i++)
            {
                languagePointers.Add(BitConverter.ToUInt32(buffer, offset + 0));
                languageSizes.Add(BitConverter.ToUInt32(buffer, offset + 4));
                languages.Add(new TextBinLanguageSection(buffer, languagePointers[i]));
                offset += 8;
            }
        }

        public override byte[] getBytes()
        {
            fixPointers();
            List<byte> bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(languageCount));
            for (int i = 0; i < languages.Count; i++)
            {
                bytes.AddRange(BitConverter.GetBytes(languagePointers[i]));
                bytes.AddRange(BitConverter.GetBytes(languageSizes[i]));
            }
            for (int i = 0; i < languages.Count; i++)
                bytes.AddRange(languages[i].getBytes());
            byte[] byteArray = new byte[bytes.Count];
            for (int i = 0; i < byteArray.Length; i++)
                byteArray[i] = bytes[i];
            return byteArray;
        }

        private void fixPointers()
        {
            uint offset = 4;
            for (int i = 0; i < languages.Count; i++)
                offset += 8;
            for (int i = 0; i < languages.Count; i++)
            {
                languagePointers[i] = offset;
                languageSizes[i] = (uint)languages[i].getBytes().Count;
                offset += languageSizes[i];
            }
        }

        public override List<string> getStrings(bool includeInvalids)
        {
            return languages[1].getStrings(includeInvalids);
        }

        public override void setStrings(List<string> strings, bool includeInvalids)
        {
            List<string> strings2 = new List<string>();
            strings2.AddRange(strings);
            languages[1].setStrings(strings, includeInvalids);
            languages[2].setStrings(strings2, includeInvalids);
        }

        // The language getStrings() reads. setStrings() deliberately copies it
        // into the second English section as well, which is what a base-text
        // reset wants; a pass that edits a handful of strings (the weapon
        // descriptions) must not rewrite the other languages with English text,
        // so it writes this one only.
        public const int EnglishLanguage = 1;

        public bool setStringsForLanguage(int language, List<string> strings, bool includeInvalids)
        {
            if (language < 0 || language >= languages.Count || strings == null)
                return false;
            languages[language].setStrings(strings, includeInvalids);
            return true;
        }
    }
}
