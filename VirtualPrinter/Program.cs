using PdfiumViewer;
using PrinterQueueWatch;
using System.Drawing.Printing;
using System.Text;
using System.Threading.Channels;
using VirtualPrinter;
using WatsonWebsocket;

var env = Environment.GetCommandLineArgs();

if (env[1] == "server")
{
    new Server();
    await Task.Delay(-1);
}
else
{
    try
    {

        WatsonWsClient client = new WatsonWsClient(env.ElementAtOrDefault(2) ?? "localhost", 56466, false);
        client.ServerConnected += ServerConnected;
        client.ServerDisconnected += ServerDisconnected;
        client.MessageReceived += MessageReceived;
        var success = client.StartWithTimeout(10);
        while (!success)
        {
            Console.WriteLine("Couldn't connect. Retrying connection");
            success = client.StartWithTimeout(10);
        }
        await Task.Delay(-1);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.ToString());
    }

    static async void MessageReceived(object sender, MessageReceivedEventArgs args)
    {
        try
        {
            var filename = new Random().Next().ToString();
            File.WriteAllBytes(filename, args.Data.ToArray());
            await Console.Out.WriteLineAsync($"Received file {filename}");
            await PrintPdf(filename);
            File.Delete(filename);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    static void ServerConnected(object sender, EventArgs args)
    {
        Console.WriteLine("Server connected");
    }

    static void ServerDisconnected(object sender, EventArgs args)
    {
        Console.WriteLine("Server disconnected. Trying to reconnect");
        var client = (WatsonWsClient)sender;
        var success = client.StartWithTimeout(10);
        while (!success)
        {
            Console.WriteLine("Couldn't connect. Retrying connection");
            success = client.StartWithTimeout(10);
        }
    }
}

static async Task PrintPdf(string filename)
{
    try
    {
        using PdfDocument doc = PdfDocument.Load(filename);

        var printDoc = doc.CreatePrintDocument();
        printDoc.PrinterSettings.PrinterName = Environment.GetCommandLineArgs().ElementAtOrDefault(3) ?? "HP Universal Printing PS";
        printDoc.PrintController = new StandardPrintController();
        await Console.Out.WriteLineAsync($"Sent {filename} for printing");
        printDoc.Print();
        await Console.Out.WriteLineAsync($"{filename} printed and deleted");
        await Task.Delay(5000);


    }
    catch (Exception e)
    {
        Console.WriteLine(e.ToString());
    }
}