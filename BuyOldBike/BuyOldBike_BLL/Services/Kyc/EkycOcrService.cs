using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Tesseract;

namespace BuyOldBike_BLL.Services.Kyc;

public sealed class EkycOcrService
{
    private readonly string _tessDataPath;
    private readonly string _lang;

    public EkycOcrService(string tessDataPath, string lang = "vie+eng")
    {
        _tessDataPath = tessDataPath;
        _lang = lang;
    }

    public string ReadText(byte[] imageBytes)
    {
        string tessDataDir = ResolveTessDataDir();
        EnsureLanguageData(tessDataDir);

        try
        {
            var engine = new TesseractEngine(tessDataDir, _lang, EngineMode.Default);
            var img = Pix.LoadFromMemory(imageBytes);
            var page = engine.Process(img);
            return page.GetText() ?? "";
        }
        catch (TesseractException ex)
        {
            throw new InvalidOperationException($"Không khởi tạo được Tesseract (tessdata: {tessDataDir}, lang: {_lang}). {ex.Message}", ex);
        }
    }

    public KycExtractResult ExtractFromCccd(byte[] frontBytes, byte[] backBytes)
    {
        string frontText = ReadText(frontBytes);
        string backText = ReadText(backBytes);
        return ExtractFromCccdText(frontText, backText);
    }

    public KycExtractResult ExtractFromCccdText(string frontText, string backText)
    {
        CccdSideExtract front = ParseCccdSide(frontText ?? "");
        CccdSideExtract back = ParseCccdSide(backText ?? "");

        string id = front.IdNumber ?? back.IdNumber ?? "";
        string dob = front.DateOfBirth ?? back.DateOfBirth ?? "";
        string fullName = front.FullName ?? back.FullName ?? "";

        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(dob))
            throw new InvalidOperationException("OCR không trích xuất đủ thông tin tối thiểu (CCCD, ngày sinh).");

        return new KycExtractResult
        {
            IdNumber = id,
            FullName = fullName,
            DateOfBirth = dob,
            Gender = front.Gender ?? back.Gender,
            Nationality = front.Nationality ?? back.Nationality,
            PlaceOfOrigin = front.PlaceOfOrigin ?? back.PlaceOfOrigin,
            PlaceOfResidence = front.PlaceOfResidence ?? back.PlaceOfResidence,
            ExpiryDate = front.ExpiryDate ?? back.ExpiryDate
        };
    }

    private sealed class CccdSideExtract
    {
        public string? IdNumber { get; set; }
        public string? FullName { get; set; }
        public string? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Nationality { get; set; }
        public string? PlaceOfOrigin { get; set; }
        public string? PlaceOfResidence { get; set; }
        public string? ExpiryDate { get; set; }
    }

    private static CccdSideExtract ParseCccdSide(string raw)
    {
        string[] rawLines = SplitLines(raw);
        string[] normLines = rawLines.Select(NormalizeForSearch).ToArray();
        string normAll = string.Join("\n", normLines);
        string rawAll = string.Join("\n", rawLines);

        return new CccdSideExtract
        {
            IdNumber = ExtractIdNumber(normAll),
            FullName = ExtractFullName(rawLines, normLines),
            DateOfBirth = ExtractDateByLabel(rawLines, normLines, new[]
            {
                "NGAY SINH",
                "DATE OF BIRTH",
                "BIRTH"
            }, allowFuture: false) ?? ExtractFirstValidDate(rawAll, allowFuture: false),
            Gender = ExtractGender(rawLines, normLines),
            Nationality = ExtractNationality(rawLines, normLines),
            PlaceOfOrigin = ExtractValueByLabel(rawLines, normLines, new[] { "QUE QUAN", "PLACE OF ORIGIN" }),
            PlaceOfResidence = ExtractPlaceOfResidence(rawLines, normLines),
            ExpiryDate = ExtractDateByLabel(rawLines, normLines, new[] { "GIA TRI DEN", "VALID THRU", "VALID UNTIL", "EXPIRY" }, allowFuture: true)
        };
    }

    private static string[] SplitLines(string text)
    {
        return (text ?? "")
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string NormalizeForSearch(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";

        string upper = input.ToUpperInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(upper.Length);
        bool lastWasSpace = false;

        foreach (char ch in upper)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;

            char c = ch == 'Đ' ? 'D' : ch;
            if (!char.IsLetterOrDigit(c)) c = ' ';

            if (c == ' ')
            {
                if (lastWasSpace) continue;
                lastWasSpace = true;
                sb.Append(' ');
                continue;
            }

            lastWasSpace = false;
            sb.Append(c);
        }

        return sb.ToString().Trim();
    }

    private static string? ExtractIdNumber(string normalizedAll)
    {
        if (string.IsNullOrWhiteSpace(normalizedAll)) return null;

        string[] labeled = Regex.Matches(
                normalizedAll,
                @"\b(?:SO|NO|ID)\s*(?:CCCD|CMND)?\s*[:\-]?\s*(\d{9,12})\b",
                RegexOptions.CultureInvariant)
            .Select(m => m.Groups[1].Value)
            .ToArray();

        string? bestLabeled = ChooseBestId(labeled);
        if (!string.IsNullOrWhiteSpace(bestLabeled)) return bestLabeled;

        string[] any = Regex.Matches(normalizedAll, @"\b\d{9,12}\b", RegexOptions.CultureInvariant)
            .Select(m => m.Value)
            .ToArray();

        return ChooseBestId(any);
    }

    private static string? ChooseBestId(string[] candidates)
    {
        if (candidates.Length == 0) return null;

        string? best12 = candidates.FirstOrDefault(c => c.Length == 12);
        if (!string.IsNullOrWhiteSpace(best12)) return best12;

        string? best9 = candidates.FirstOrDefault(c => c.Length == 9);
        if (!string.IsNullOrWhiteSpace(best9)) return best9;

        return candidates[0];
    }

    private static string? ExtractFullName(string[] rawLines, string[] normLines)
    {
        for (int i = 0; i < normLines.Length; i++)
        {
            string norm = normLines[i];
            if (!Regex.IsMatch(norm, @"\b(HO VA TEN|HO TEN|FULL NAME|SURNAME|GIVEN NAME)\b", RegexOptions.CultureInvariant)) continue;

            string sameLineCandidate = StripNameLabelPrefix(rawLines[i]);
            string cleanedSame = FixCommonVietnameseSurnameOcr(CleanName(sameLineCandidate));
            if (IsPlausibleName(cleanedSame)) return cleanedSame;

            for (int j = i + 1; j < rawLines.Length && j <= i + 3; j++)
            {
                string next = FixCommonVietnameseSurnameOcr(CleanName(rawLines[j]));
                if (IsPlausibleName(next)) return next;
            }
        }

        return null;
    }

    private static string? ExtractGender(string[] rawLines, string[] normLines)
    {
        for (int i = 0; i < normLines.Length; i++)
        {
            if (!ContainsAnyLabel(normLines[i], new[] { "GIOI TINH", "SEX" })) continue;

            string afterLabel = ExtractAfterAnyLabel(rawLines[i], @"GIỚI\s*TÍNH|GIOI\s*TINH|SEX");
            afterLabel = CutAtFirstSeparator(afterLabel);

            string normalized = NormalizeForSearch(afterLabel);
            var match = Regex.Match(normalized, @"\b(NAM|NU|MALE|FEMALE)\b", RegexOptions.CultureInvariant);
            if (match.Success) return MapGender(match.Value);

            string cleaned = CleanLooseValue(afterLabel);
            if (!string.IsNullOrWhiteSpace(cleaned)) return cleaned;
        }

        return null;
    }

    private static string? ExtractNationality(string[] rawLines, string[] normLines)
    {
        for (int i = 0; i < normLines.Length; i++)
        {
            if (!ContainsAnyLabel(normLines[i], new[] { "QUOC TICH", "NATIONALITY" })) continue;

            string afterLabel = rawLines[i].Contains(':', StringComparison.Ordinal) || rawLines[i].Contains('：', StringComparison.Ordinal)
                ? ExtractValueAfterLastColon(rawLines[i])
                : ExtractAfterAnyLabel(rawLines[i], @"QUỐC\s*TỊCH|QUOC\s*TICH|NATIONALITY");

            afterLabel = CutAtFirstSeparator(afterLabel);
            string cleaned = CleanLooseValue(afterLabel);
            if (string.IsNullOrWhiteSpace(cleaned)) continue;

            cleaned = cleaned.TrimStart('/', '\\').Trim();
            cleaned = Regex.Replace(
                cleaned,
                @"(?i)^(NATIONALITY)\s*[:\-]*\s*",
                "",
                RegexOptions.CultureInvariant).Trim();

            string normalized = NormalizeForSearch(cleaned);
            if (normalized is "NAM" or "NU" or "MALE" or "FEMALE") continue;

            return cleaned;
        }

        return null;
    }

    private static string? ExtractPlaceOfResidence(string[] rawLines, string[] normLines)
    {
        for (int i = 0; i < normLines.Length; i++)
        {
            if (!ContainsAnyLabel(normLines[i], new[] { "NOI THUONG TRU", "PLACE OF RESIDENCE", "ADDRESS" })) continue;

            string candidate = rawLines[i].Contains(':', StringComparison.Ordinal) || rawLines[i].Contains('：', StringComparison.Ordinal)
                ? ExtractValueAfterLastColon(rawLines[i])
                : ExtractAfterAnyLabel(rawLines[i], @"NƠI\s*THƯỜNG\s*TRÚ|NOI\s*THUONG\s*TRU|PLACE\s*OF\s*RESIDENCE|ADDRESS");

            candidate = RemoveEnglishResidenceLabel(candidate);
            candidate = CleanLooseValue(candidate);

            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(candidate)) parts.Add(candidate);

            for (int j = i + 1; j < rawLines.Length && j <= i + 5; j++)
            {
                if (IsOtherFieldLine(normLines[j])) break;

                string next = RemoveEnglishResidenceLabel(rawLines[j]);
                next = CleanLooseValue(next);
                if (string.IsNullOrWhiteSpace(next)) continue;
                parts.Add(next);
            }

            if (parts.Count > 0) return string.Join("\n", parts);
        }

        return null;
    }

    private static string RemoveEnglishResidenceLabel(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        var match = Regex.Match(
            value,
            @"(?i)\b(\/\s*)?(PLACE\s*OF\s*RESIDENCE|ADDRESS)\b",
            RegexOptions.CultureInvariant);

        string output = match.Success ? value[..match.Index] : value;
        output = output.Trim();
        output = output.TrimEnd('/', '\\', '-', ':', ';', ',').Trim();
        return output;
    }

    private static bool IsResidenceIncomplete(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        string norm = NormalizeForSearch(value);
        if (norm is "PLACE OF RESIDENCE" or "ADDRESS") return true;
        if (norm.EndsWith(" PLACE OF RESIDENCE", StringComparison.Ordinal)) return true;

        string[] tokens = norm.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0) return true;

        var incompleteFirstWords = new HashSet<string>(StringComparer.Ordinal)
        {
            "THON", "TO", "AP", "KHU", "PHUONG", "XA", "HUYEN", "QUAN", "TP", "TINH"
        };

        return tokens.Length == 1 && incompleteFirstWords.Contains(tokens[0]);
    }

    private static bool IsOtherFieldLine(string normalizedLine)
    {
        if (string.IsNullOrWhiteSpace(normalizedLine)) return false;

        string[] labels =
        {
            "HO VA TEN",
            "HO TEN",
            "FULL NAME",
            "NGAY SINH",
            "DATE OF BIRTH",
            "GIOI TINH",
            "SEX",
            "QUOC TICH",
            "NATIONALITY",
            "QUE QUAN",
            "PLACE OF ORIGIN",
            "GIA TRI DEN",
            "VALID THRU",
            "VALID UNTIL",
            "EXPIRY",
            "SO",
            "CCCD",
            "CMND",
            "CAN CUOC",
            "ID"
        };

        foreach (string label in labels)
        {
            if (normalizedLine.Contains(label, StringComparison.Ordinal)) return true;
        }

        return false;
    }

    private static string? ExtractValueByLabel(string[] rawLines, string[] normLines, string[] labels)
    {
        for (int i = 0; i < normLines.Length; i++)
        {
            if (!ContainsAnyLabel(normLines[i], labels)) continue;

            string sameLine = ExtractValueFromRawLine(rawLines[i]);
            string cleaned = CleanLooseValue(sameLine);
            if (!string.IsNullOrWhiteSpace(cleaned)) return cleaned;

            if (i + 1 < rawLines.Length)
            {
                string next = CleanLooseValue(rawLines[i + 1]);
                if (!string.IsNullOrWhiteSpace(next)) return next;
            }
        }

        return null;
    }

    private static string? ExtractDateByLabel(string[] rawLines, string[] normLines, string[] labels, bool allowFuture)
    {
        for (int i = 0; i < normLines.Length; i++)
        {
            if (!ContainsAnyLabel(normLines[i], labels)) continue;

            string sameLineValue = ExtractValueFromRawLine(rawLines[i]);
            string normalized;
            if (TryNormalizeDate(FindDateInText(sameLineValue), allowFuture, out normalized)) return normalized;
            if (TryNormalizeDate(FindDateInText(rawLines[i]), allowFuture, out normalized)) return normalized;

            if (i + 1 < rawLines.Length && TryNormalizeDate(FindDateInText(rawLines[i + 1]), allowFuture, out normalized)) return normalized;
            if (i + 2 < rawLines.Length && TryNormalizeDate(FindDateInText(rawLines[i + 2]), allowFuture, out normalized)) return normalized;
        }

        return null;
    }

    private static bool ContainsAnyLabel(string normalizedLine, string[] labels)
    {
        foreach (string label in labels)
        {
            if (normalizedLine.Contains(label, StringComparison.Ordinal)) return true;
        }
        return false;
    }

    private static string ExtractValueFromRawLine(string rawLine)
    {
        if (string.IsNullOrWhiteSpace(rawLine)) return "";
        string[] parts = rawLine.Split(new[] { ':', '：' }, 2, StringSplitOptions.TrimEntries);
        return parts.Length == 2 ? parts[1] : rawLine;
    }

    private static string ExtractValueAfterLastColon(string rawLine)
    {
        if (string.IsNullOrWhiteSpace(rawLine)) return "";
        int idx = rawLine.LastIndexOf(':');
        int idxAlt = rawLine.LastIndexOf('：');
        int last = Math.Max(idx, idxAlt);
        return last >= 0 && last + 1 < rawLine.Length ? rawLine[(last + 1)..].Trim() : ExtractValueFromRawLine(rawLine);
    }

    private static string ExtractAfterAnyLabel(string rawLine, string labelsPattern)
    {
        if (string.IsNullOrWhiteSpace(rawLine)) return "";
        var match = Regex.Match(
            rawLine,
            $@"(?i)\b(?:{labelsPattern})\b.*?(?:[:：\-]\s*)?(.+)$",
            RegexOptions.CultureInvariant);
        return match.Success ? match.Groups[1].Value.Trim() : ExtractValueFromRawLine(rawLine);
    }

    private static string CutAtFirstSeparator(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        int idx = value.IndexOfAny(new[] { '.', ',', ';', '\n' });
        return idx >= 0 ? value[..idx].Trim() : value.Trim();
    }

    private static string MapGender(string token)
    {
        return token switch
        {
            "NAM" => "Nam",
            "NU" => "Nữ",
            "MALE" => "Nam",
            "FEMALE" => "Nữ",
            _ => token
        };
    }

    private static string StripNameLabelPrefix(string rawLine)
    {
        if (string.IsNullOrWhiteSpace(rawLine)) return "";

        string afterColon = ExtractValueFromRawLine(rawLine);
        string stripped = Regex.Replace(
            afterColon,
            @"(?i)\b(HỌ\s*VÀ\s*TÊN|HO\s*VA\s*TEN|HỌ\s*TÊN|HO\s*TEN|FULL\s*NAME|SURNAME|GIVEN\s*NAME)\b\s*[:\-]*\s*",
            "",
            RegexOptions.CultureInvariant);

        return stripped.Trim();
    }

    private static bool IsPlausibleName(string cleanedName)
    {
        if (string.IsNullOrWhiteSpace(cleanedName)) return false;

        string norm = NormalizeForSearch(cleanedName);
        if (Regex.IsMatch(norm, @"\b(HO VA TEN|HO TEN|FULL NAME|SURNAME|GIVEN NAME)\b", RegexOptions.CultureInvariant)) return false;

        string[] tokens = norm.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length < 2) return false;

        var stop = new HashSet<string>(StringComparer.Ordinal) { "VA", "TEN", "HO", "FULL", "NAME", "SURNAME", "GIVEN" };
        if (tokens.All(t => stop.Contains(t))) return false;
        if (tokens.Length == 2 && tokens[0] == "VA" && tokens[1] == "TEN") return false;

        return true;
    }

    private static string? ExtractFirstValidDate(string normalizedAll, bool allowFuture)
    {
        string[] matches = Regex.Matches(normalizedAll, @"\b\d{2}\D+\d{2}\D+\d{4}\b", RegexOptions.CultureInvariant)
            .Select(m => m.Value)
            .ToArray();

        foreach (string match in matches)
        {
            string normalized;
            if (TryNormalizeDate(match, allowFuture, out normalized)) return normalized;
        }

        return null;
    }

    private static string FindDateInText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        var match = Regex.Match(text, @"\b\d{2}\D+\d{2}\D+\d{4}\b", RegexOptions.CultureInvariant);
        return match.Success ? match.Value : "";
    }

    private static bool TryNormalizeDate(string dateText, bool allowFuture, out string normalized)
    {
        normalized = "";
        if (string.IsNullOrWhiteSpace(dateText)) return false;

        string cleaned = Regex.Replace(dateText.Trim(), @"[^\d]", "/", RegexOptions.CultureInvariant);
        cleaned = Regex.Replace(cleaned, @"\/{2,}", "/", RegexOptions.CultureInvariant).Trim('/');
        if (!DateTime.TryParseExact(cleaned, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt)) return false;

        DateTime today = DateTime.Today;
        if (dt.Year < 1900) return false;
        if (!allowFuture && dt > today) return false;
        if (today.Year - dt.Year > 130) return false;

        normalized = dt.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        return true;
    }

    private static string CleanName(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        string s = Regex.Replace(value, @"[\d_]+", " ", RegexOptions.CultureInvariant);
        s = Regex.Replace(s, @"[^\p{L}\s]+", " ", RegexOptions.CultureInvariant);
        s = Regex.Replace(s, @"\s+", " ", RegexOptions.CultureInvariant).Trim();
        return s;
    }

    private static string FixCommonVietnameseSurnameOcr(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "";

        string[] parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return name;

        var surnameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "DANG", "ĐẶNG" },
        { "DO", "ĐỖ" },
        { "DUONG", "DƯƠNG" },
        { "DINH", "ĐINH" },
        { "NGUYEN", "NGUYỄN" },
        { "TRAN", "TRẦN" },
        { "LE", "LÊ" },
        { "PHAM", "PHẠM" },
        { "HOANG", "HOÀNG" },
        { "HUYNH", "HUỲNH" },
        { "PHAN", "PHAN" },
        { "VU", "VŨ" },
        { "VO", "VÕ" }
    };

        if (surnameMap.TryGetValue(parts[0], out string? fixedSurname) && !string.IsNullOrWhiteSpace(fixedSurname))
        {
            bool isUpper = parts[0].All(ch => !char.IsLetter(ch) || char.IsUpper(ch));
            parts[0] = isUpper ? fixedSurname : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(fixedSurname.ToLower());
        }

        return string.Join(' ', parts);
    }

    private static string CleanLooseValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        string s = Regex.Replace(value, @"[_]+", " ", RegexOptions.CultureInvariant);
        s = Regex.Replace(s, @"\s+", " ", RegexOptions.CultureInvariant).Trim();
        return s;
    }

    private string ResolveTessDataDir()
    {
        if (string.IsNullOrWhiteSpace(_tessDataPath))
            throw new InvalidOperationException("Thiếu đường dẫn tessdata.");

        if (Path.IsPathRooted(_tessDataPath))
            return _tessDataPath;

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, _tessDataPath));
    }

    private void EnsureLanguageData(string tessDataDir)
    {
        if (!Directory.Exists(tessDataDir))
            throw new InvalidOperationException($"Không tìm thấy thư mục tessdata: {tessDataDir}");

        string[] langs = _lang.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        string[] missingFiles = langs
            .Select(l => Path.Combine(tessDataDir, $"{l}.traineddata"))
            .Where(p => !File.Exists(p))
            .Select(p => Path.GetFileName(p) ?? "")
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToArray();

        if (missingFiles.Length == 0) return;

        throw new InvalidOperationException(
            $"Thiếu file traineddata: {string.Join(", ", missingFiles)}. " +
            $"Hãy copy vào thư mục: {tessDataDir}");
    }
}
