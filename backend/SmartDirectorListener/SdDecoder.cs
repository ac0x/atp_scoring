using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Backend.SmartDirectorListener.Models;

namespace Backend.SmartDirectorListener;

public class SdDecoder
{
    private static readonly Regex RxPacket = new("<Packet>(?<b64>[^<]+)</Packet>", RegexOptions.Compiled);
    private static readonly Regex RxCData = new("<!\\[CDATA\\[(?<c>.*?)\\]>>", RegexOptions.Singleline | RegexOptions.Compiled);

    public IEnumerable<DecodedSnapshot> DecodePackets(string xmlPayload)
    {
        var hadCData = false;

        foreach (var cdata in ExtractAllCData(xmlPayload))
        {
            hadCData = true;
            foreach (var snapshot in DecodeBase64Packets(cdata))
            {
                yield return snapshot;
            }
        }

        if (!hadCData)
        {
            foreach (var snapshot in DecodeBase64Packets(xmlPayload))
            {
                yield return snapshot;
            }
        }
    }

    private IEnumerable<DecodedSnapshot> DecodeBase64Packets(string text)
    {
        foreach (Match packet in RxPacket.Matches(text))
        {
            var raw = packet.Groups["b64"].Value.Trim();
            if (string.IsNullOrEmpty(raw))
            {
                continue;
            }

            var ascii = TryBase64ToUtf8(raw);
            if (string.IsNullOrEmpty(ascii))
            {
                continue;
            }

            yield return DecodeSnapshot(ascii);
        }
    }

    private static IEnumerable<string> ExtractAllCData(string xml)
    {
        foreach (Match match in RxCData.Matches(xml))
        {
            var value = match.Groups["c"].Value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                yield return value;
            }
        }
    }

    private static string TryBase64ToUtf8(string input)
    {
        try
        {
            var data = Convert.FromBase64String(input);
            return Encoding.UTF8.GetString(data);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string Slice(string source, int start, int length)
    {
        if (string.IsNullOrEmpty(source) || length <= 0)
        {
            return string.Empty;
        }

        if (start < 0 || start >= source.Length)
        {
            return string.Empty;
        }

        if (start + length > source.Length)
        {
            length = source.Length - start;
        }

        return source.Substring(start, length);
    }

    private static string MapPoint(string raw)
    {
        if (raw == null)
        {
            return string.Empty;
        }

        var trimmed = raw.Trim();
        if (trimmed.Length == 0)
        {
            return string.Empty;
        }

        var normalized = trimmed.Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
        return normalized switch
        {
            "00" or "0" => "0",
            "15" => "15",
            "30" => "30",
            "40" => "40",
            "AD" or "A" or "41" or "45" => "Ad",
            _ => trimmed
        };
    }

    private static string NormalizeServer(string raw)
    {
        var token = (raw ?? string.Empty).Trim();
        if (token.Length == 0)
        {
            return string.Empty;
        }

        return token switch
        {
            "1" or "A" => "A",
            "2" or "B" => "B",
            _ => token
        };
    }

    private static DecodedSnapshot DecodeSnapshot(string ascii)
    {
        var snapshot = new DecodedSnapshot
        {
            Raw = ascii,
            PlayerA1 = Slice(ascii, 10, 20).Trim(),
            PlayerB1 = Slice(ascii, 30, 20).Trim(),
            PlayerA2 = Slice(ascii, 50, 20).Trim(),
            PlayerB2 = Slice(ascii, 70, 20).Trim(),
            BestOf = Slice(ascii, 303, 1).Trim(),
            MatchClock = Slice(ascii, 213, 8).Trim(),
            PointsA = MapPoint(Slice(ascii, 340, 2)),
            PointsB = MapPoint(Slice(ascii, 342, 2)),
            Set1A = Slice(ascii, 344, 2).Trim(),
            Set1B = Slice(ascii, 346, 2).Trim(),
            Set2A = Slice(ascii, 355, 2).Trim(),
            Set2B = Slice(ascii, 357, 2).Trim(),
            Set3A = Slice(ascii, 366, 2).Trim(),
            Set3B = Slice(ascii, 368, 2).Trim(),
            Server = NormalizeServer(Slice(ascii, 399, 1))
        };

        return snapshot;
    }
}