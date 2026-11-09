using System;
using System.Globalization;

public static class SerialProtocol
{
    public const int BaudRate = 9600;
    public const int MinAngle = 0;
    public const int MaxAngle = 180;
    public const char LineTerminator = '\n';

    private const string StatePrefix = "S:";
    private const string CommandPrefix = "A:";
    private const string PotentiometerSource = "POT";
    private const string DigitalSource = "DIG";
    private const char FieldSeparator = ',';
    private const int StateFieldCount = 2;
    private const int AngleFieldIndex = 0;
    private const int SourceFieldIndex = 1;

    public static string BuildAngleCommand(int angle)
    {
        return CommandPrefix + angle.ToString(CultureInfo.InvariantCulture) + LineTerminator;
    }

    public static bool TryParseState(string message, out int angle, out bool fromPotentiometer)
    {
        angle = 0;
        fromPotentiometer = false;

        string trimmedMessage = message.Trim();
        if (!trimmedMessage.StartsWith(StatePrefix, StringComparison.Ordinal))
        {
            return false;
        }

        string[] fields = trimmedMessage.Substring(StatePrefix.Length).Split(FieldSeparator);
        if (fields.Length != StateFieldCount)
        {
            return false;
        }

        string source = fields[SourceFieldIndex].Trim();
        bool hasKnownSource = source == PotentiometerSource || source == DigitalSource;
        bool hasValidAngle = int.TryParse(fields[AngleFieldIndex], NumberStyles.Integer,
                                 CultureInfo.InvariantCulture, out angle)
                             && angle >= MinAngle && angle <= MaxAngle;

        fromPotentiometer = source == PotentiometerSource;
        return hasKnownSource && hasValidAngle;
    }
}
