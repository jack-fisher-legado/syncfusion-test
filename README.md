# syncfusion-test

This repo is a simple test of SyncFusion's capabilities for 
PDF digital signing.

You can use the razor pages Index to instantly download a file, or you can call the API endpoint
to see the latest functionality e.g. multi page signing

/api/fileapi/post

{
    "FileId": 1,
    "Certificate": "123abc",
    "Signatures": [
        {
            "SignerId": 99,
            "IpAddress": "89:000:111",
            "Coordinates": {
                "x": 10,
                "y": 500
            },
            "SignerName": "Jack Fisher"
        },
        {
            "SignerId": 10,
            "IpAddress": "89:999:121",
            "Coordinates": {
                "x": 10,
                "y": 1000
            },
            "SignerName": "Lucy Hasareallylongnamethatyoujustcantbeleiveisthislongbutitjustmightbe"
        }
    ]
}