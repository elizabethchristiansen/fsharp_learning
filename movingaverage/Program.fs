//val req : string list list =
//  [["Date"; "Open"; "High"; "Low"; "Close"; "Volume"; "Adj Close"];
//   ["2010-04-21"; "31.33"; "31.50"; "31.23"; "31.33"; "55343100"; "30.83"];
//   ["2010-04-20"; "31.22"; "31.44"; "31.13"; "31.36"; "52199500"; "30.86"];
//   ...

open System
open System.IO
open System.Xml
open System.Text
open System.Net
open System.Globalization

let makeUrl symbol (dfrom:DateTime) (dto:DateTime) = 
    //Uses the not-so-known chart-data:
    let monthfix (d:DateTime)= (d.Month-1).ToString()
    new Uri("http://ichart.finance.yahoo.com/table.csv?s=" + symbol +
        "&e=" + dto.Day.ToString() + "&d=" + monthfix(dto) + "&f=" + dto.Year.ToString() +
        "&g=d&b=" + dfrom.Day.ToString() + "&a=" + monthfix(dfrom) + "&c=" + dfrom.Year.ToString() +
        "&ignore=.csv")


let fetch (url : Uri) = 
    let req = WebRequest.Create (url) :?> HttpWebRequest
    use stream = req.GetResponse().GetResponseStream()
    use reader = new StreamReader(stream)
    reader.ReadToEnd()

let reformat (response:string) = 
    let split (mark:char) (data:string) = 
        data.Split(mark)
    response |> split '\n'
    |> Array.filter (fun f -> f<>"") 
    |> Array.map (split ',')
    
let getRequest uri = (fetch >> reformat) uri

let makeMove (stock:string [] []) =
    let avgList dur (array:string [] []) = Array.mapi ( fun i (x:string []) ->
        [| Array.sub array ( max ( i - dur ) 1 ) i |> Array.averageBy( fun ( elem:string [] ) -> ( elem.[4] |> float ) ); x.[4] |> float |] ) array
    
    Array.mapi ( fun i (x:string []) -> [| Array.sub stock ( max ( i - 5 ) 1 ) i |> Array.averageBy( fun ( elem:string [] ) -> ( elem.[4] |> float ) ); x.[4] |> float |] ) stock
    //Array.fold( fun acc (elem:float []) ->
    //    if elem.[0] >= elem.[1] then 1000.0 * elem.[1] else -1000.0 * elem.[1]
    //) 10000.0 ( avgList 15 stock )

let stocks = makeUrl "^GSPC" (new DateTime(2016, 1, 1)) (new DateTime(2017, 1, 1)) |> getRequest |> makeMove
                    