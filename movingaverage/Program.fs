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

let movingAverage dur (stock:string [] []) =
    Array.mapi ( fun i (x:string []) -> 
        let curDur = min dur i
        if i = 0
        then [| 0.0; 0.0 |] 
        else [| stock.[(1+i-curDur)..i] |> Array.averageBy( fun ( elem:string [] ) -> ( elem.[4] |> float ) ); x.[4] |> float |] ) stock
    |> Array.fold( fun [|n;p|] (elem:float []) -> 
       if elem.[0] >= elem.[1] then [|0.0; p + ( n * elem.[1] )|] else [|n + ( p / elem.[1] ); 0.0|]) [|0.0; 100000.0|]

let gspc = makeUrl "^GSPC" (new DateTime(2002, 1, 1)) (new DateTime(2017, 1, 5)) |> getRequest
let msft = makeUrl "MSFT" (new DateTime(2016, 1, 1)) (new DateTime(2017, 1, 5)) |> getRequest

let stockToProfit amount (data:string [] []) = 
    let datalen = data.Length
    amount * ( data.[datalen-1].[4] |> float )

let profitByWindow stocks = Array.map( fun f -> 
    let pnl = movingAverage f stocks
    [ f |> float; max ( stockToProfit pnl.[0] stocks ) pnl.[1] ] ) [| 5..5..120 |]

profitByWindow msft