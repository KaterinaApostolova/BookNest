using System.Globalization;

namespace BookNest.Services;

public enum QuoteCollection
{
    Register,
    Login,
    Logout
}

public class QuoteService
{
    private readonly Random _random = new();

    private static readonly IReadOnlyDictionary<QuoteCollection, string[]> EnglishQuotes = new Dictionary<QuoteCollection, string[]>
    {
        [QuoteCollection.Register] = new[]
        {
            "Every journey begins with a single page.",
            "A new chapter starts today.",
            "Stories become even better when they are shared.",
            "Every library begins with a single book. Every reader begins with a single page.",
            "Welcome to the place where stories continue long after the final chapter.",
            "Some books become memories. Some memories become home.",
            "You don't just collect books. You collect pieces of yourself.",
            "Today you're not joining a website. You're joining thousands of stories.",
            "Every reader deserves a place where excitement never has to stay silent.",
            "Every story deserves a place to be remembered.",
            "Every great reader was once someone opening their first book.",
            "Today begins a journey that doesn't end with the last page.",
            "Some stories change us. Others help us find ourselves.",
            "Welcome to a place where every reader belongs.",
            "Every book opens a new door.",
            "“ There is no friend as loyal as a book. ” — Ernest Hemingway",
            "“ A room without books is like a body without a soul. ” — Marcus Tullius Cicero",
            "“ The only thing you absolutely have to know is the location of the library.” — Albert Einstein",
            "“ We read to know we are not alone. ” — C. S. Lewis"
        },
        [QuoteCollection.Login] = new[]
        {
            "Every ending deserves a conversation.",
            "Stories live beyond their pages.",
            "Another adventure begins today.",
            "The last page is only the beginning.",
            "Every reader deserves a place to belong.",
            "Some stories stay with us forever. Let's discover another one.",
            "Every page is easier to turn when someone shares the journey.",
            "Your bookshelf has been waiting for you.",
            "Every return begins with curiosity.",
            "The conversations you missed are waiting between the pages.",
            "Some stories ask to be remembered. Others ask to be shared.",
            "Books patiently wait. So do the people who love them.",
            "Your reading journey never really paused. It simply waited.",
           "“ Not all those who wander are lost. ” — J. R. R. Tolkien",
           "“ It does not do to dwell on dreams and forget to live. ” — J. K. Rowling",
           "“ There is always room for another story. ” — Hans Christian Andersen"
        },
        [QuoteCollection.Logout] = new[]
        {
            "Until your next story.",
            "Books will be waiting.",
            "Every goodbye is another beginning.",
            "The next page is always here.",
            "Every ending is simply another place to begin.",
            "We'll be here when your next story finds you.",
            "Take today's stories with you.",
            "Until the next chapter.",
            "Because no story should end with the last page.",
            "Every goodbye leaves a bookmark for the next chapter.",
            "The best stories are worth returning to.",
            "Take today's emotions with you.",
            "BookNest will always have another page waiting." ,
            "“ Tomorrow is a new day. ” — L. M. Montgomery",
            "“ The world was hers for the reading. ” — Betty Smith",
            "“ There is more treasure in books than in all the pirate's loot. ” — Walt Disney",
            "“ A reader lives a thousand lives before he dies. ” — George R. R. Martin"
        }
    };

    private static readonly IReadOnlyDictionary<QuoteCollection, string[]> BulgarianQuotes = new Dictionary<QuoteCollection, string[]>
    {
        [QuoteCollection.Register] = new[]
        {
            "Всяко пътешествие започва с една страница.",
            "Днес започва нова глава.",
            "Историите стават още по-хубави, когато се споделят.",
            "Всяка библиотека започва с една книга. Всеки читател започва с една страница.",
            "Добре дошъл на мястото, където историите продължават дълго след последната глава.",
            "Някои книги стават спомени. Някои спомени стават дом.",
            "Не просто колекционираш книги. Колекционираш частици от себе си.",
            "Днес не се присъединяваш към уебсайт. Присъединяваш се към хиляди истории.",
            "Всеки читател заслужава място, където вълнението никога не трябва да остава премълчано.",
            "Всяка история заслужава място, където да бъде запомнена.",
            "Всеки велик читател някога е бил някой, отварящ първата си книга.",
            "Днес започва пътешествие, което не свършва с последната страница.",
            "Някои истории ни променят. Други ни помагат да открием себе си.",
            "Добре дошъл на място, където всеки читател принадлежи.",
            "Всяка книга отваря нова врата.",
            "„ Няма по-верен приятел от книгата. “ — Ърнест Хемингуей",
            "„ Стая без книги е като тяло без душа. “ — Марк Тулий Цицерон",
            "„ Единственото нещо, което наистина трябва да знаеш, е местоположението на библиотеката. “ — Алберт Айнщайн",
            "„ Четем, за да знаем, че не сме сами. “ — К. С. Луис"
        },
        [QuoteCollection.Login] = new[]
        {
            "Всеки край заслужава разговор.",
            "Историите живеят отвъд страниците си.",
            "Днес започва още едно приключение.",
            "Последната страница е само началото.",
            "Всеки читател заслужава място, на което принадлежи.",
            "Някои истории остават с нас завинаги. Нека открием още една.",
            "Всяка страница се обръща по-лесно, когато някой споделя пътя.",
            "Твоята библиотека те очакваше.",
            "Всяко завръщане започва с любопитство.",
            "Разговорите, които пропусна, те чакат между страниците.",
            "Някои истории искат да бъдат запомнени. Други искат да бъдат споделени.",
            "Книгите чакат търпеливо. Както и хората, които ги обичат.",
            "Твоето читателско пътешествие никога не е спирало наистина. Просто е чакало.",
            "„ Не всички, които се скитат, са изгубени. “ — Дж. Р. Р. Толкин",
            "„ Не бива да се отдаваме на мечти и да забравяме да живеем. “ — Дж. К. Роулинг",
            "„ Винаги има място за още една история. “ — Ханс Кристиан Андерсен"
        },
        [QuoteCollection.Logout] = new[]
        {
            "До следващата ти история.",
            "Книгите ще чакат.",
            "Всяко сбогуване е ново начало.",
            "Следващата страница винаги е тук.",
            "Всеки край е просто ново място за начало.",
            "Ще сме тук, когато следващата ти история те открие.",
            "Вземи днешните истории със себе си.",
            "До следващата глава.",
            "Защото никоя история не бива да свършва с последната страница.",
            "Всяко сбогуване оставя отметка за следващата глава.",
            "Най-добрите истории си струва да ги преоткриваш.",
            "Вземи днешните емоции със себе си.",
            "BookNest винаги ще има още една страница, която те чака.",
            "„ Утре е нов ден. “ — Л. М. Монтгомъри",
            "„ Светът беше неин за четене. “ — Бети Смит",
            "„ В книгите има повече съкровища, отколкото във всичкото плячкосано злато на пиратите. “ — Уолт Дисни",
            "„ Читателят живее хиляда живота, преди да умре. “ — Джордж Р. Р. Мартин"
        }
    };

    public string GetQuote(QuoteCollection collection)
    {
        var isBulgarian = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "bg";
        var source = isBulgarian ? BulgarianQuotes : EnglishQuotes;

        if (!source.TryGetValue(collection, out var values) || values.Length == 0)
        {
            return isBulgarian ? "Добре дошъл в BookNest." : "Welcome to BookNest.";
        }

        var index = _random.Next(values.Length);
        return values[index];
    }
}
