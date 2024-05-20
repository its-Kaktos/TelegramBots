using System;
using System.Globalization;

namespace TelegramBots.Web.Common.ExtensionMethods;

public static class DateTimeOffsetOffsetExtensions
{
    #region Private fileds

    private static readonly CultureInfo PersianCultureInfo = CultureInfo.ReadOnly(new CultureInfo("fa-Ir"));
    private const string ShamsiFullDateTimeOffsetFormat = "yyyy/MM/dd HH:mm:ss";
    private const string ShamsiWithOutTimeDateTimeOffsetFormat = "yyyy/MM/dd";
    private const string ShamsiWithOutTimeForPictureNameDateTimeOffsetFormat = "yyyy-MM-dd";

    #endregion

    /// <summary>
    /// If <paramref name="dateTimeOffset"/> is null will return an empty string, else
    /// would return <paramref name="dateTimeOffset"/> as the following template :
    /// 1399/02/03 16:23:04
    /// </summary>
    /// <param name="dateTimeOffset">Date time</param>
    /// <returns>Date time in persian date time, E.g : 1399/02/03 16:23:04</returns>
    public static string ToShamsiFull(this DateTimeOffset? dateTimeOffset)
    {
        return dateTimeOffset == null ? string.Empty : dateTimeOffset.Value.ToString(ShamsiFullDateTimeOffsetFormat, PersianCultureInfo);
    }

    /// <summary>
    /// Would return <paramref name="dateTimeOffset"/> as the following template :
    /// 1399/02/03 16:23:04
    /// </summary>
    /// <param name="dateTimeOffset">Date time</param>
    /// <returns>Date time in persian date time, E.g : 1399/02/03 16:23:04</returns>
    public static string ToShamsiFull(this DateTimeOffset dateTimeOffset)
    {
        return dateTimeOffset.ToString(ShamsiFullDateTimeOffsetFormat, PersianCultureInfo);
    }

    /// <summary>
    /// If <paramref name="dateTimeOffset"/> is null will return an empty string, else
    /// would return <paramref name="dateTimeOffset"/> as the following template : 1399/02/03
    /// </summary>
    /// <param name="dateTimeOffset">Date time</param>
    /// <returns>Date time in persian date time, E.g : 1399/02/03</returns>
    public static string ToShamsiWithoutTime(this DateTimeOffset? dateTimeOffset)
    {
        return dateTimeOffset == null ? string.Empty : dateTimeOffset.Value.ToString(ShamsiWithOutTimeDateTimeOffsetFormat, PersianCultureInfo);
    }

    /// <summary>
    /// Would return <paramref name="dateTimeOffset"/> as the following template : 1399/02/03
    /// </summary>
    /// <param name="dateTimeOffset">Date time</param>
    /// <returns>Date time in persian date time, E.g : 1399/02/03</returns>
    public static string ToShamsiWithoutTime(this DateTimeOffset dateTimeOffset)
    {
        return dateTimeOffset.ToString(ShamsiWithOutTimeDateTimeOffsetFormat, PersianCultureInfo);
    }

    /// <summary>
    /// Would return <paramref name="dateTimeOffset"/> as the following template : 1399-02-03
    /// </summary>
    /// <param name="dateTimeOffset">Date time</param>
    /// <returns>Date time in persian date time, E.g : 1399-02-03</returns>
    public static string ToShamsiWithoutTimeForPictureName(this DateTimeOffset dateTimeOffset)
    {
        return dateTimeOffset.ToString(ShamsiWithOutTimeForPictureNameDateTimeOffsetFormat, PersianCultureInfo);
    }

    /// <summary>
    /// Will return the <paramref name="unixTimeStamp"/> as <see cref="DateTimeOffset"/>.
    /// <para>
    /// IMPORTANT : the <paramref name="unixTimeStamp"/> should be in UTC and the
    /// returned <see cref="DateTimeOffset"/> is in local time.
    /// </para>
    /// </summary>
    /// <param name="unixTimeStamp">The utc time stamp</param>
    /// <returns>A <see cref="DateTimeOffset"/> that represents the given <paramref name="unixTimeStamp"/></returns>
    public static DateTimeOffset UnixTimeStampToDateTimeOffset(double unixTimeStamp)
    {
        return DateTimeOffset.UnixEpoch.AddSeconds(unixTimeStamp).ToLocalTime();
    }
}