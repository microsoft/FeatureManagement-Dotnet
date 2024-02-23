var loadingBars = [];
var widths = [];

for (var i = 0; i < loadingBars.length; i++)
{
    widths.push(0);
}

function tick() {
    for (var i = 0; i < loadingBars.length; i++)
    {
        console.log(loadingBars[i].variant);
        console.log(commonNames.length);

        let newProgress = 0;
        if (loadingBars[i].variant > 0) {
            newProgress = 1 + (4 / (100 / loadingBars[i].variant)) + loadingBars[i].randomVariance;
        }

        loadingBars[i].progress += newProgress;

        if (loadingBars[i].progress >= 100)
        {
            loadingBars[i].progress = 0;
            loadingBars[i].variant = 0;
            GetNewUser(loadingBars[i]);
        }

        loadingBars[i].element.style.width = loadingBars[i].progress + "%";
    }
}

setInterval(tick, 100);