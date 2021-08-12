// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

function toggleEvent(item) {

    var expanded = $(item).closest('.accordian-toggle').attr('aria-expanded');

    console.log("Expanded " + expanded);
    if (expanded == 'false' || expanded == undefined)
    {
        $(item).removeClass('fa-angle-double-down');
        $(item).addClass('fa-angle-double-up');
    }
    else {
        $(item).removeClass('fa-angle-double-up');
        $(item).addClass('fa-angle-double-down');
    }
}