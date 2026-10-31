using System.Globalization;

namespace BookNest.Services;

public static class GenreTranslator
{
    private static readonly IReadOnlyDictionary<string, string> BulgarianNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Fantasy"] = "Фентъзи",
        ["Science Fiction"] = "Научна фантастика",
        ["Sci-Fi"] = "Научна фантастика",
        ["Classic"] = "Класика",
        ["Classics"] = "Класика",
        ["Horror"] = "Ужаси",
        ["Romance"] = "Романтика",
        ["Mystery"] = "Мистерия",
        ["Thriller"] = "Трилър",
        ["Crime"] = "Криминален",
        ["Non-Fiction"] = "Нехудожествена литература",
        ["Nonfiction"] = "Нехудожествена литература",
        ["Biography"] = "Биография",
        ["Autobiography"] = "Автобиография",
        ["Historical Fiction"] = "Историческа проза",
        ["History"] = "История",
        ["Young Adult"] = "Тийн литература",
        ["Children"] = "Детска литература",
        ["Children's"] = "Детска литература",
        ["Poetry"] = "Поезия",
        ["Drama"] = "Драма",
        ["Adventure"] = "Приключенски",
        ["Dystopian"] = "Дистопия",
        ["Fairy Tale"] = "Приказка",
        ["Self-Help"] = "Самопомощ",
        ["Philosophy"] = "Философия",
        ["Psychology"] = "Психология",
        ["Comedy"] = "Комедия",
        ["Humor"] = "Хумор",
        ["Satire"] = "Сатира",
        ["War"] = "Военен роман",
        ["Western"] = "Уестърн",
        ["Short Stories"] = "Разкази",
        ["Graphic Novel"] = "Графичен роман",
        ["Memoir"] = "Мемоари",
        ["Travel"] = "Пътеписи",
        ["Business"] = "Бизнес",
        ["Science"] = "Наука",
        ["Religion"] = "Религия",
        ["Spirituality"] = "Духовност",
        ["Politics"] = "Политика",
        ["True Crime"] = "Истинско престъпление",
        ["Paranormal"] = "Паранормално",
        ["Supernatural"] = "Свръхестествено",
        ["Contemporary"] = "Съвременна проза",
        ["Literary Fiction"] = "Художествена литература",
        ["Gothic"] = "Готически роман"
    };

    /// <summary>
    /// Returns the Bulgarian name for a known genre when the current UI culture is Bulgarian.
    /// Falls back to the original (English) name for unmapped genres or non-Bulgarian culture.
    /// </summary>
    public static string Localize(string englishName)
    {
        if (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName != "bg")
        {
            return englishName;
        }

        return BulgarianNames.TryGetValue(englishName.Trim(), out var bulgarianName)
            ? bulgarianName
            : englishName;
    }
}
