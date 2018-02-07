var fs = require("fs");

const FILE_NAME = "items-final.json";

var loaded = false;
var ITEMS = [];

function isSameItem(item1, item2){
    return item1["id"] === item2["id"]
            && item1["color"] === item2["color"]
            && item1["price"] === item2["price"]
            && item1["checkout-price"] === item2["checkout-price"]
            && item1["img"] === item2["img"];
}

function load(){
    if (loaded)
        return;
    loaded = true;
    ITEMS = JSON.parse(fs.readFileSync(FILE_NAME));
}

function save(){
    fs.writeFile(FILE_NAME, JSON.stringify(ITEMS), 'utf8');
}

function list(){
    load();
    return ITEMS.map(i => i.id);
}

function get(id){
    load();
    return ITEMS.find(i => i.id == id);
}

function set(item){
    load();
    const idx = ITEMS.findIndex(i => i.id == item.id);
    if (idx != -1){
        if (isSameItem(item, ITEMS[idx]))
            return true;
        ITEMS.splice(idx, 1);
    }
    item.time = new Date().getTime();
    ITEMS.push(item);
    
    save();

    return true;
}

function remove(id){
    load();

    const idx = ITEMS.findIndex(i => i.id == id);
    if (idx == -1)
        return true;

    ITEMS.splice(idx, 1);
    
    save();

    return true;
}

module.exports = {
    list: list,
    get: get,
    set: set,
    remove: remove
};
