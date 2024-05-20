using System;
using System.Globalization;

namespace TelegramBots.Web.Common.ExtensionMethods;

public static class DateTimeExtensions
{
    #region Private fileds

    private static readonly CultureInfo PersianCultureInfo = CultureInfo.ReadOnly(new CultureInfo("fa-Ir"));
    private const string ShamsiFullDateTimeFormat = "yyyy/MM/dd HH:mm:ss";
    private const string ShamsiWithOutTimeDateTimeFormat = "yyyy/MM/dd";
    private const string ShamsiWithOutTimeForPictureNameDateTimeFormat = "yyyy-MM-dd";

    #endregion

    /// <summary>
    /// If <paramref name="dateTime"/> is null will return an empty string, else
    /// would return <paramref name="dateTime"/> as the following template :
    /// 1399/02/03 16:23:04
    /// </summary>
    /// <param name="dateTime">Date time</param>
    /// <returns>Date time in persian date time, E.g : 1399/02/03 16:23:04</returns>
    public static string ToShamsiFull(this DateTime? dateTime)
    {
        return dateTime == null ? string.Empty : dateTime.Value.ToString(ShamsiFullDateTimeFormat, PersianCultureInfo);
    }

    /// <summary>
    /// Would return <paramref name="dateTime"/> as the following template :
    /// 1399/02/03 16:23:04
    /// </summary>
    /// <param name="dateTime">Date time</param>
    /// <returns>Date time in persian date time, E.g : 1399/02/03 16:23:04</returns>
    public static string ToShamsiFull(this DateTime dateTime)
    {
        return dateTime.ToString(ShamsiFullDateTimeFormat, PersianCultureInfo);
    }

    /// <summary>
    /// If <paramref name="dateTime"/> is null will return an empty string, else
    /// would return <paramref name="dateTime"/> as the following template : 1399/02/03
    /// </summary>
    /// <param name="dateTime">Date time</param>
    /// <returns>Date time in persian date time, E.g : 1399/02/03</returns>
    public static string ToShamsiWithoutTime(this DateTime? dateTime)
    {
        return dateTime == null ? string.Empty : dateTime.Value.ToString(ShamsiWithOutTimeDateTimeFormat, PersianCultureInfo);
    }

    /// <summary>
    /// Would return <paramref name="dateTime"/> as the following template : 1399/02/03
    /// </summary>
    /// <param name="dateTime">Date time</param>
    /// <returns>Date time in persian date time, E.g : 1399/02/03</returns>
    public static string ToShamsiWithoutTime(this DateTime dateTime)
    {
        return dateTime.ToString(ShamsiWithOutTimeDateTimeFormat, PersianCultureInfo);
    }

    /// <summary>
    /// Would return <paramref name="dateTime"/> as the following template : 1399-02-03
    /// </summary>
    /// <param name="dateTime">Date time</param>
    /// <returns>Date time in persian date time, E.g : 1399-02-03</returns>
    public static string ToShamsiWithoutTimeForPictureName(this DateTime dateTime)
    {
        return dateTime.ToString(ShamsiWithOutTimeForPictureNameDateTimeFormat, PersianCultureInfo);
    }

    /// <summary>
    /// Will return the <paramref name="unixTimeStamp"/> as <see cref="DateTime"/>.
    /// <para>
    /// IMPORTANT : the <paramref name="unixTimeStamp"/> should be in UTC and the
    /// returned <see cref="DateTime"/> is in local time.
    /// </para>
    /// </summary>
    /// <param name="unixTimeStamp">The utc time stamp</param>
    /// <returns>A <see cref="DateTime"/> that represents the given <paramref name="unixTimeStamp"/></returns>
    public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
    {
        return DateTime.UnixEpoch.AddSeconds(unixTimeStamp).ToLocalTime();
    }
}