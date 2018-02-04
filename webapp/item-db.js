var fs = require("fs");

var ITEMS = JSON.parse(fs.readFileSync("items-final.json"));

function isSameItem(item1, item2){
    return item1.id == item2.id
        && item1.color == item2.color
        && item1.price == item2.price
        && item1.checkoutPrice == item2.checkoutPrice
        && item1.img === item2.img;
}

function list(){
    return ITEMS.map(i => i.id);
}

function get(id){
    return ITEMS.find(i => i.id == id);
}

function set(item){
    const idx = ITEMS.findIndex(i => i.id == item.id);
    if (idx != -1){
        if (isSameItem(item, ITEMS[idx]))
            return true;
        ITEMS.splice(idx, 1);
    }
    item.time = new Date().getTime();
    ITEMS.push(item);
    return true;
}

module.exports = {
    list: list,
    get: get,
    set: set
};
