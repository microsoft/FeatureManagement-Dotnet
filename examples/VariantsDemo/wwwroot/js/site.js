var loadingBars = [];
var widths = [];

for (var i = 0; i < loadingBars.length; i++)
{
    widths.push(0);
}

// Handles one tick of loading bars
function tick() {
    for (var i = 0; i < loadingBars.length; i++)
    {
        let newProgress = 0;

        // If we're just starting to load, enable transition animations
        if (loadingBars[i].progress == 0 && loadingBars[i].userInfo.Variant > 0) {
            loadingBars[i].element.classList.remove("disable-transitions");
        }

        // How much progress will we make
        if (loadingBars[i].userInfo.Variant > 0) {
            newProgress = 2 * (loadingBars[i].userInfo.Variant / 100) * loadingBars[i].randomVariance;
        }

        // Apply progress
        loadingBars[i].progress += newProgress;

        // If the loading bar finished
        if (loadingBars[i].progress >= 100) {
            // Stop loading
            loadingBars[i].element.classList.add("disable-transitions");
            loadingBars[i].progress = 0;
            loadingBars[i].userInfo.Variant = 0;

            // Get a new user
            GetNewUser(loadingBars[i]);
        }

        // Update loading bar
        loadingBars[i].element.style.width = loadingBars[i].progress + "%";
    }
}

// Tick 10 times a second, could be lowered, but the accuracy of timed duration will suffer
setInterval(tick, 100);


// Finishes a user and grabs a new user
function GetNewUser(loadingBar) {
    fetch('./GetUserInfo', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({
            UserInfo: loadingBar.userInfo,
            Duration: new Date() - loadingBar.start
        })
    })
        .then(response => response.json())
        .then(data => {
            console.log(data);

            BindAndStoreData(loadingBar.container, loadingBar.index, data);
        })
        .catch((error) => {
            console.error('Error:', error);
        });
}


// Binds user data to a loading bar
function BindAndStoreData(container, index, data) {
    let name = container.querySelector(".progress-bar-name");
    let variant = container.querySelector(".progress-bar-variant");
    let progress = container.querySelector(".progress");

    name.innerHTML = data.Username;
    variant.innerHTML = data.VariantName;
    progress.style.backgroundColor = variantColors[data.VariantName];

    // Store Loading Bar Object
    loadingBars[index] = {
        start: new Date(),
        index: index,
        container: container,
        element: container.querySelector(".progress"),
        userInfo: data,
        progress: 0,
        randomVariance: 1 + (.50 - Math.random() / 2)
    };
}