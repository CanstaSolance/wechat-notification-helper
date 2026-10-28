using System.IO.Pipes;
using System.Text;

namespace WeChatNotifier;

internal sealed class LocalControl(Func<string,Task<string>> handle) : IDisposable
{
    readonly CancellationTokenSource stopping=new();
    Task? work;
    internal void Start()=>work=Serve();
    async Task Serve()
    {
        while(!stopping.IsCancellationRequested)
        {
            try
            {
                using var pipe=new NamedPipeServerStream(AppState.PipeName,PipeDirection.InOut,1,PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous|PipeOptions.CurrentUserOnly,1024,4096);
                await pipe.WaitForConnectionAsync(stopping.Token);
                using var timeout=CancellationTokenSource.CreateLinkedTokenSource(stopping.Token);timeout.CancelAfter(2500);
                using var reader=new StreamReader(pipe,Encoding.UTF8,false,1024,true);
                using var writer=new StreamWriter(pipe,new UTF8Encoding(false),1024,true){AutoFlush=true};
                var buffer=new char[128];int count=0;
                while(count<buffer.Length)
                {
                    int read=await reader.ReadAsync(buffer.AsMemory(count,1),timeout.Token);if(read==0)break;
                    if(buffer[count++]=='\n')break;
                }
                string command=new(buffer,0,count);command=command.TrimEnd('\r','\n');
                string result=Allowed(command)?await handle(command).WaitAsync(timeout.Token):"INVALID_COMMAND";
                await writer.WriteLineAsync(result.AsMemory(),timeout.Token);
                if(command=="quit")await handle("shutdown");
            }
            catch(OperationCanceledException){}
            catch(Exception ex){AppState.Log("ipc-error.json",new{type=ex.GetType().Name});await Task.Delay(100);}
        }
    }
    internal static bool Allowed(string value)=>value is "settings" or "status" or "quit" or "pause" or "resume" or "test" ||
        value.Length==41&&value.StartsWith("activate/",StringComparison.Ordinal)&&Guid.TryParseExact(value[9..],"N",out _);
    internal static async Task<string?> Send(string command)
    {
        if(!Allowed(command))return null;
        try
        {
            using var timeout=new CancellationTokenSource(2000);
            using var pipe=new NamedPipeClientStream(".",AppState.PipeName,PipeDirection.InOut,PipeOptions.Asynchronous|PipeOptions.CurrentUserOnly);
            await pipe.ConnectAsync(timeout.Token);
            using var reader=new StreamReader(pipe,Encoding.UTF8,false,1024,true);
            using var writer=new StreamWriter(pipe,new UTF8Encoding(false),1024,true){AutoFlush=true};
            await writer.WriteLineAsync(command.AsMemory(),timeout.Token);
            var reply=new char[4096];int count=0;
            while(count<reply.Length)
            {
                int read=await reader.ReadAsync(reply.AsMemory(count,1),timeout.Token);if(read==0)break;
                if(reply[count++]=='\n')return new string(reply,0,count).TrimEnd('\r','\n');
            }
            return null;
        }
        catch{return null;}
    }
    public void Dispose(){stopping.Cancel();/* Async server unwinds without blocking the UI thread. */}
}
