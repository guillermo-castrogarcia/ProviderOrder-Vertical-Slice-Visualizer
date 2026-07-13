document.addEventListener('DOMContentLoaded', function() {
    const toggler = document.getElementsByClassName("treeview-node");

    for (let i = 0; i < toggler.length; i++) {
        console.log("added event listener to " + toggler[i]);
       
    }
});