function getPureUrl() {
    return window.location.href.split('#')[0];
}

window.onload = async () => {
    const textArea = document.getElementById("textArea");
    const generateButton = document.getElementById("createLinkBtn");
    const resetButton = document.getElementById("resetBtn");

    adjustHeight(textArea);
    textArea.onkeyup = _ => adjustHeight(textArea);

    generateButton.onclick = onButtonClick;

    resetButton.onclick = async _ => {
        // Lazy but like who cares? :)
        await navigator.clipboard.writeText(textArea.value);
        document.location.replace(getPureUrl());
    };
    
    if (document.location.hash.startsWith("#v"))
        await loadSecret(); // Time to decrypt!
};

async function loadSecret() {
    const textArea = document.getElementById("textArea");
    const hash = document.location.hash.split("#");
    
    if(hash.length !== 5) {
        showAlert("danger", "Invalid view link, sorry!");
        enableReadonlyMode();
        return;
    }
    
    const id = hash[2];
    const iv = hash[3];
    const key = hash[4];

    const response = await fetch(`api/bn/${id}`);

    const responseModel = await response.json();

    if(!responseModel.success) {
        showAlert("danger", `Failed to retrieve note. Maybe it was already read?`);
        return;
    }

    const encryptedBytes = Uint8Array.from(atob(responseModel.result), c => c.charCodeAt(0));
    const ivBytes = Uint8Array.from(atob(iv), c => c.charCodeAt(0));
    const keyBytes = Uint8Array.from(atob(key), c => c.charCodeAt(0));

    const aesCbc = new aesjs.ModeOfOperation.cbc(keyBytes, ivBytes);
    const decryptedBytes = aesCbc.decrypt(encryptedBytes);

    textArea.value = aesjs.utils.utf8.fromBytes(decryptedBytes).trim();
    
    enableReadonlyMode();
}

async function onButtonClick(event) {
    const textArea = document.getElementById("textArea");
    const secretValue = textArea.value;

    if (secretValue === "") {
        showAlert("danger", "Secret can't be empty!");
        return;
    }

    hideAlert();

    const request = {
        text: secretValue
    };
    
    const response = await fetch("api/bn/create", {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify(request)
    });
    
    const responseModel = await response.json();
    
    if(!responseModel.success) {
        showAlert("danger", `Failed to create note. Error code: ${responseModel.error.code}. ${responseModel.error.message}`);
        return;
    }
    
    setResponseUrl(responseModel);
    enableReadonlyMode();
    
    showAlert("success", "Your note has been created. Keep in mind that it will auto-delete itself in 12 hours.");
}

function showAlert(type, text) {
    const alert = document.getElementById("errorAlert");
    
    alert.className = `alert alert-${type} subtitle`;
    alert.innerText = text;
    alert.style.display = "block";
}

function hideAlert() {
    const alert = document.getElementById("errorAlert");

    alert.style.display = "none";
}

function enableReadonlyMode() {
    const generateButton = document.getElementById("createLinkBtn");
    const resetButton = document.getElementById("resetBtn");
    const textArea = document.getElementById("textArea");

    generateButton.style.display = "none";
    resetButton.style.display = "block";
    textArea.setAttribute("readonly", "");
}

// Uhh.. I stole that.
// https://stackoverflow.com/a/17259991/12030195
function adjustHeight(el){
    const newHeight = (el.scrollHeight > el.clientHeight) ? (Math.min(el.scrollHeight, 260)) : 180;
    el.style.height = `${newHeight}px`;
}

function setResponseUrl(responseModel) {
    const data = responseModel.result;
    const id = data.id;
    const iv = data.iv;
    const key = data.key;

    const pureUrl = getPureUrl();
    const newUrl = `${pureUrl}#v#${id}#${iv}#${key}`;
    
    const textArea = document.getElementById("textArea");
    textArea.value = newUrl;
}