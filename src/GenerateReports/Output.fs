module TradeOps.Output

open System
open System.IO
open System.Reflection
open RazorEngine.Configuration
open RazorEngine.Templating
open TradeOps.Models

//-------------------------------------------------------------------------------------------------

let private folderOutput = Environment.GetEnvironmentVariable("UserProfile") + @"\Desktop\Output\"
let private folderStyles = Path.Combine(folderOutput, "css")

let private templateResolver name =
    use stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name)
    if (stream = null) then failwith ("Cannot load resource: " + name)
    use reader = new StreamReader(stream)
    reader.ReadToEnd()

let private config = new TemplateServiceConfiguration()
config.TemplateManager <- new DelegateTemplateManager(fun x -> templateResolver x)
config.CachingProvider <- new DefaultCachingProvider(fun x -> ignore x)
config.DisableTempFileLocking <- true

let private service = RazorEngineService.Create(config)

//-------------------------------------------------------------------------------------------------

let private write model (filenameOutput : string) =

    Directory.CreateDirectory(folderOutput) |> ignore
    Directory.CreateDirectory(folderStyles) |> ignore

    let filename = "Report.css"
    let contents = service.RunCompile(filename, typeof<unit>, ())
    let path = Path.Combine(folderStyles, filename)
    File.WriteAllText(path, contents)

    let filename = filenameOutput
    let contents = service.RunCompile(filename, model.GetType(), model)
    let path = Path.Combine(folderOutput, filename)
    File.WriteAllText(path, contents)

//-------------------------------------------------------------------------------------------------

let writeTransactionListing (model : TransactionListing.Model) =
    write model "TransactionListing.html"

let writeStatementPositions (model : StatementPositions.Model) =
    write model "StatementPositions.html"

let writeStatementStops (model : StatementStops.Model) =
    write model "StatementStops.html"
