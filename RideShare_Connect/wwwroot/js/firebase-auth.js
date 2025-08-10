import { initializeApp } from 'https://www.gstatic.com/firebasejs/10.12.0/firebase-app.js';
import { getAuth, GoogleAuthProvider, signInWithPopup } from 'https://www.gstatic.com/firebasejs/10.12.0/firebase-auth.js';

const firebaseConfig = {
    apiKey: "AIzaSyB-EVmUBv8LJVPJ2dS-JyLwkyIIXw74QZg",
    authDomain: "rideshareconnect-962ca.firebaseapp.com",
    projectId: "rideshareconnect-962ca",
    storageBucket: "rideshareconnect-962ca.firebasestorage.app",
    messagingSenderId: "251646945927",
    appId: "1:251646945927:web:c35dad132ce3bc9e386622"
};

const app = initializeApp(firebaseConfig);
const auth = getAuth(app);
const provider = new GoogleAuthProvider();

window.googleSignIn = async function () {
    try {
        const result = await signInWithPopup(auth, provider);
        const credential = GoogleAuthProvider.credentialFromResult(result);
        const idToken = credential.idToken;

        const profilePicture = result.user.photoURL || 'https://avatar.iran.liara.run/public/boy?username=Ash';
        const fullName = result.user.displayName || '';

        const response = await fetch('/UserAccount/GoogleFirebaseSignIn', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ idToken, profilePicture, fullName })
        });

        if (response.ok) {
            const data = await response.json();
            if (data.redirectUrl) {
                window.location.href = data.redirectUrl;
            }
        } else {
            console.error('Server responded with error', await response.text());
        }
    } catch (error) {
        console.error('Firebase Google sign-in failed:', error);
    }
}
