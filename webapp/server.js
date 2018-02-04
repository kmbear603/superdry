var express = require("express");
var bodyParser = require("body-parser");
var app = express();
var itemDB = require("./item-db.js");

app.use(express.static("."));
app.use(bodyParser.json());

app.get("/item", (req, res)=>{
    const id_list = itemDB.list();
    res.status(200).send(JSON.stringify(id_list));
});

app.get("/item/:id", (req, res)=>{
    const itm = itemDB.get(req.params.id);
    if (!itm)
        res.status(404).send("unknown ID " + req.params.id);
    else
        res.status(200).send(JSON.stringify(itm));
});

app.post("/item/:id", (req, res)=>{
    const ok = itemDB.set(req.body);
    if (!ok)
        res.status(500).send("failed");
    else
        res.status(200).send("OK");
});

app.listen(8080);
