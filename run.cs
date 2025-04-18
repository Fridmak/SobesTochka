using System.Text.RegularExpressions;


class HotelCapacity
{
    // Я тут еще везде var поставил где компилятор нормально определяет тип переменной
    // Класс тут был лишним. зачем по ссылкам работать когда можно в стеке хранить эти 24 байта
    struct Guest
    {
        public readonly string Name;
        public readonly string CheckIn;
        public readonly string CheckOut;

        public Guest(string name, string checkIn, string checkOut)
        {
            Name = name;
            CheckIn = checkIn;
            CheckOut = checkOut;
        }
    }
    static bool CheckCapacity(int maxCapacity, List<Guest> guests)
    {
        var events = new List<(DateTime date, int change)>();

        foreach (var guest in guests)
        {
            DateTime checkInDate = DateTime.ParseExact(guest.CheckIn, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            DateTime checkOutDate = DateTime.ParseExact(guest.CheckOut, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

            events.Add((checkInDate, 1)); 
            events.Add((checkOutDate, -1));
        }

        events.Sort((a, b) =>
        {
            var dateComparison = a.date.CompareTo(b.date);
            if (dateComparison != 0)
                return dateComparison;
            return a.change.CompareTo(b.change);
        });

        var currentGuests = 0;
        foreach (var (date, change) in events)
        {
            currentGuests += change;
            if (currentGuests > maxCapacity)
                return false;
        }

        return true;
    }
    
    // Там у вас в тестах лишний перенос. так что будем еще проверять корректность ввода без фанатизма
    static void Main()
    {
        string line;
        
        while (string.IsNullOrWhiteSpace(line = Console.ReadLine())) { }
        var maxCapacity = int.Parse(line);
        
        while (string.IsNullOrWhiteSpace(line = Console.ReadLine())) { }
        var n = int.Parse(line);
        
        var guests = new List<Guest>();
        for (int i = 0; i < n; i++)
        {
            while (string.IsNullOrWhiteSpace(line = Console.ReadLine())) { }
            var guest = ParseGuest(line);
            guests.Add(guest);
        }
        
        var result = CheckCapacity(maxCapacity, guests);

        Console.WriteLine(result);
    }
    
    static Guest ParseGuest(string json)
    {
        var name = "";
        var dateIn = "";
        var dateOut = "";

        var nameMatch = Regex.Match(json, "\"name\"\\s*:\\s*\"([^\"]+)\"");
        if (nameMatch.Success)
            name = nameMatch.Groups[1].Value;

        var checkInMatch = Regex.Match(json, "\"check-in\"\\s*:\\s*\"([^\"]+)\"");
        if (checkInMatch.Success)
            dateIn = checkInMatch.Groups[1].Value;
        
        var checkOutMatch = Regex.Match(json, "\"check-out\"\\s*:\\s*\"([^\"]+)\"");
        if (checkOutMatch.Success)
            dateOut = checkOutMatch.Groups[1].Value;
        
        return new Guest(name, dateIn, dateOut);
    }
}