module Newsletter.API.Routes

open Microsoft.AspNetCore.Http

open Giraffe

open Http.Handler
open Http.Auth

let parsingError err = RequestErrors.BAD_REQUEST err

let webApp staticToken : HttpFunc -> HttpContext -> HttpFuncResult =
    choose [ subRoute
                 "/api"
                 (choose
                    [ staticBasic staticToken
                                >=>
                                subRoute "/subscribers" //http://localhost:5000/api/subscribers
                                   (choose
                                        [
                                             POST >=> route "/create" >=> handleAddSubscriberAsync
                                             PUT >=> routef "/update/%s" handleUpdateSubscriberAsync              
                                             GET
                                             >=> choose [ route "" >=> handleGetSubscribersAsync
                                                          routef "/get/%i" handleGetByIdAsync
                                                          routef "/get/%s" handleGetByEmailAsync ]
                                             DELETE >=> routef "/cancel/%s" handleDeleteSubscriberAsync
                                                ]
                                            )
                                ]
                            )
                    ]

// {
// 	"Name": "test",
// 	"Email": "valid@tion.ok"
// }
