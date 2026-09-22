using System;
using System.IO;
using System.Reflection;

class Program {
    static void Main() {
        var dllPath = @""d:\TrainTicket\TrainTicket.WinForms\bin\Debug\net8.0-windows\BCrypt.Net-Next.dll"";
        var asm = Assembly.LoadFrom(dllPath);
        var type = asm.GetType(""BCrypt.Net.BCrypt"");
        var method = type.GetMethod(""HashPassword"", new[] { typeof(string) });
        var hash = method.Invoke(null, new object[] { ""Admin@123"" });
        Console.WriteLine(hash);
    }
}
