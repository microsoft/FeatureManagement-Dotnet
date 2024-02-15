
var elements = document.getElementsByClassName("progress");
var widths = [];

for (var i = 0; i < elements.length; i++)
{
    widths.push(0);
}

function tick() {
    for (var i = 0; i < elements.length; i++)
    {
        widths[i]++;
        elements[i].style.width = widths[i] + "%";

        if (widths[i] >= 100)
        {
            widths[i] = 0;
        }
    }
}

setInterval(tick, 100);