using System;
using System.IO.Ports;
using System.Linq;

namespace PublishCOMPortData;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("Enter Virtual COM port:");
        var comPort = Console.ReadLine();
        var port = new SerialPort(comPort, 9600, Parity.None, 8, StopBits.One);
        port.Open();

        while (true)
        {
            Console.WriteLine("Enter byte array:");
            var input = Console.ReadLine();
            var data = input.Split('-').Select(x => (byte)int.Parse(x)).ToArray();

            port.Write(data, 0, data.Length);
        }

        // 6-49-82-71-23-32-48-48-46-48-48-48-32-48-48-46-48-48-32-48-48-48-50-51-32-50-48-46-57-48-32-48-48-48-48-48-48-48-48-48-50-66-3
        // 1RG 00.000 00.00 00023 20.90 0000000002B

        // 6-49-82-71-23-32-49-51-46-48-56-51-32-50-54-46-52-51-32-48-56-56-50-49-32-48-48-46-48-48-32-48-50-55-55-48-48-48-48-48-53-67-3
        // 1RG 13.083 26.43 08821 00.00 0277000005C

        // 6-49-82-71-23-32-49-51-46-48-56-51-32-50-54-46-52-51-32-48-56-56-50-50-32-48-48-46-48-48-32-48-50-55-55-48-48-48-48-48-53-68-3
        // 1RG 13.083 26.43 08822 00.00 0277000005D
    }
}
