module CrierBot.Aes

open System
open System.Text
open System.Security.Cryptography
open Microsoft.AspNetCore.WebUtilities

let fromBase64Url (str: string) = str |> WebEncoders.Base64UrlDecode

let toBase64Url (bytes: byte []) = bytes |> WebEncoders.Base64UrlEncode

let toBytes (str: string) = str |> Encoding.UTF8.GetBytes

let private createCipher keyBase64 =
    let iv = "HR$2pIjHHR$2pIjH"
    let cipher = Aes.Create()
    cipher.Mode <- CipherMode.CBC // Ensure the integrity of the ciphertext if using CBC

    cipher.Padding <- PaddingMode.ISO10126
    cipher.Key <- Convert.FromBase64String(keyBase64)
    cipher.IV <- Encoding.UTF8.GetBytes iv
    cipher

let createKey () =
    let crypto = Aes.Create()
    crypto.KeySize <- 128
    crypto.BlockSize <- 128

    try
        crypto.GenerateKey()
        let keyGenerated = crypto.Key
        Convert.ToBase64String(keyGenerated)
    finally
        crypto.Dispose()

let encrypt key (input: string) =
    let cipher = createCipher (key)

    let cryptTransform = cipher.CreateEncryptor()
    let plaintext = Encoding.UTF8.GetBytes(input)

    try
        cryptTransform.TransformFinalBlock(plaintext, 0, plaintext.Length)
        |> toBase64Url
    finally
        cryptTransform.Dispose()

let decrypt key encryptedText =
    let cipher = createCipher (key)

    let cryptTransform = cipher.CreateDecryptor()
    let encryptedBytes = fromBase64Url encryptedText

    try
        cryptTransform.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length)
        |> Encoding.UTF8.GetString
    finally
        cryptTransform.Dispose()
