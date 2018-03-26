module App

open Elmish

open Fable.Core.JsInterop
open Fable.Helpers.React
open Fable.Helpers.React.Props

open Fulma
open Fulma.Layouts
open Fulma.Elements
open Fulma.Elements.Form
open Fulma.Extra.FontAwesome
open Fulma.Components
open Fulma.BulmaClasses

open Shared
open Fable.PowerPack
open Elmish

/// The data model driving the view
type Report =
    { Location : LocationResponse
      Crimes : CrimeResponse array
      Weather : WeatherResponse }

type ServerState = Idle | Loading | ServerError of string

type Model =
    { Postcode : string
      ValidationError : string option
      ServerState : ServerState
      Report : Report option }

/// The different types of messages in the system
type Msg =
    | GetReport
    | PostcodeChanged of string
    | GotReport of Report
    | ErrorMsg of exn

/// The init function is called to start the message pump with an initial view.
let init () = 
    { Postcode = null
      Report = None
      ValidationError = None
      ServerState = Idle }, Cmd.ofMsg (PostcodeChanged "")

let getResponse postcode = promise {
    let! location = Fetch.fetchAs<LocationResponse> (sprintf "/api/distance/%s" postcode) []
    let! crimes = Fetch.fetchAs<CrimeResponse array> (sprintf "api/crime/%s" postcode) []
    let! weather = Fetch.fetchAs<WeatherResponse> (sprintf "api/weather/%s" postcode) []
    return { Location = location; Crimes = crimes; Weather = weather } }
 
/// The update function knows how to update the model given a message.
let update msg model =
    match model, msg with
    | { ValidationError = None; Postcode = postcode }, GetReport ->
        { model with ServerState = Loading }, Cmd.ofPromise getResponse postcode GotReport ErrorMsg
    | _, GetReport -> model, Cmd.none
    | _, GotReport response ->
        { model with
            ValidationError = None
            Report = Some response
            ServerState = Idle }, Cmd.none
    | _, PostcodeChanged p ->
        let p = p.ToUpper()
        { model with
            Postcode = p
            ValidationError =
              if Validation.validatePostcode p then None
              else Some "Invalid postcode."
            ServerState = Idle
            Report = None }, Cmd.none
    | _, ErrorMsg e -> { model with ServerState = ServerError e.Message }, Cmd.none

/// The view function knows how to render the UI given a model, as well as to dispatch new messages based on user actions.
let view model dispatch =
    div [] [
        Navbar.navbar [ Navbar.Color IsPrimary ] [
            Navbar.Item.div [] [
                Heading.h2 [] [ str "Location Review!" ] ]
            ]
        
        Container.container [] [
            yield 
                Field.div [] [
                    Label.label [] [ str "Postcode" ]
                    Control.div [ Control.HasIconLeft; Control.HasIconRight ] [
                        Input.text [ Input.Placeholder "Ex: EC2A 4NE"; Input.Value model.Postcode; Input.Props [ OnChange (fun ev -> dispatch (PostcodeChanged !!ev.target?value)) ] ]
                        Icon.faIcon [ Icon.Size IsSmall; Icon.IsLeft ] [ Fa.icon Fa.I.Building ]
                        (match model with
                         | { ServerState = Loading } -> span [ Class "icon is-small is-right" ] [ i [ Class "fa fa-spinner faa-spin animated" ] [] ] 
                         | { ValidationError = Some _ } -> Icon.faIcon [ Icon.Size IsSmall; Icon.IsRight ] [ Fa.icon Fa.I.Exclamation ]
                         | { ValidationError = None } -> Icon.faIcon [ Icon.Size IsSmall; Icon.IsRight ] [ Fa.icon Fa.I.Check ])
                    ]
                    Help.help
                       [ Help.Color (if model.ValidationError.IsNone then IsSuccess else IsDanger) ]
                       [ str (model.ValidationError |> Option.defaultValue "") ]
                ]
            yield
                Field.div [ Field.IsGrouped ] [
                    Control.div [] [
                        Button.button
                            [ Button.IsFullwidth; Button.Color IsPrimary; Button.OnClick (fun _ -> dispatch GetReport); Button.Disabled model.ValidationError.IsSome ]
                            [ str "Submit" ] ] 
                ]

            match model with
            | { Report = None; ServerState = (Idle | Loading) } -> ()
            | { ServerState = ServerError error } -> yield Field.div [] [ str error ]
            | { Report = Some model } ->
                yield
                    Card.card [ ] [
                        Card.header [ ] [
                            Card.Header.title [ Card.Header.Title.IsCentered ] [ str (sprintf "%s (%s - %s)" model.Location.Postcode model.Location.Location.Region model.Location.Location.Town) ]
                            Card.Header.icon [ ] [ i [ ClassName "fa fa-address-card" ] [ ] ]
                            ]
                        Card.content [ ] [
                            Content.content [ ]
                                [ iframe [
                                    Style [ Height 310; Width 410 ]
                                    Src (sprintf "https://www.bing.com/maps/embed?h=300&w=400&cp=%f~%f&lvl=11&typ=s&sty=r&src=SHELL&FORM=MBEDV8" model.Location.Location.LatLong.Latitude model.Location.Location.LatLong.Longitude) ]
                                    []
                                ]
                            ]
                        ]
        ]

        Footer.footer [] [
            Content.content
                [ Content.CustomClass Bulma.Properties.Alignment.HasTextCentered ]
                [ safeComponents ]
        ]
    ]