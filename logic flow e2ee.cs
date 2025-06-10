/*
* Phía server:
1.	Thêm trường EncryptedSessionKeys vào model Conversation
2.	Thêm trường EncryptedSessionKey vào ChatMessage để hỗ trợ gửi khóa phiên lần đầu (nếu cần)

3. lưu dạng pair (userId, encryptedSessionKey) vào EncryptedSessionKeys 
khi tạo cuộc trò chuyện mới hoặc khi có người dùng mới tham gia cuộc trò chuyện

4. Khi người dùng vào một boxchat, 
Server trả về publicKeys của các participants (trừ chính user) 
và session key đã mã hóa dành cho user đó

* Xử lý phía client:
** Khi tạo cuộc trò chuyện mới
1. Tạo 1 khóa phiên và tạo các bản mã hóa khóa phiên bằng 
từng khóa công khai của các thành viên trong cuộc trò chuyện,
sau đó gửi lên server

a. Sinh session key (AES)
import javax.crypto.KeyGenerator
import javax.crypto.SecretKey

fun generateSessionKey(): SecretKey {
    val keyGen = KeyGenerator.getInstance("AES")
    keyGen.init(256)
    return keyGen.generateKey()
}

b. Mã hóa session key bằng public key (RSA)
import java.security.PublicKey
import javax.crypto.Cipher
import android.util.Base64

fun encryptSessionKeyWithPublicKey(sessionKey: SecretKey, publicKey: PublicKey): String {
    val cipher = Cipher.getInstance("RSA/ECB/PKCS1Padding")
    cipher.init(Cipher.ENCRYPT_MODE, publicKey)
    val encrypted = cipher.doFinal(sessionKey.encoded)
    return Base64.encodeToString(encrypted, Base64.NO_WRAP)
}

c. Chuẩn bị dữ liệu gửi lên server
data class EncryptedSessionKeyInfo(
    val userId: Int,
    val encryptedSessionKey: String
)

d. Tổng hợp logic gửi lên server
// Giả sử bạn đã có danh sách publicKeys: Map<Int, String> (userId -> base64 publicKey)
val sessionKey = generateSessionKey()
val encryptedSessionKeys = mutableListOf<EncryptedSessionKeyInfo>()

for ((userId, publicKeyBase64) in publicKeys) {
    val publicKeyBytes = Base64.decode(publicKeyBase64, Base64.DEFAULT)
    val keySpec = java.security.spec.X509EncodedKeySpec(publicKeyBytes)
    val keyFactory = java.security.KeyFactory.getInstance("RSA")
    val publicKey = keyFactory.generatePublic(keySpec)
    val encryptedSessionKey = encryptSessionKeyWithPublicKey(sessionKey, publicKey)
    encryptedSessionKeys.add(EncryptedSessionKeyInfo(userId, encryptedSessionKey))
}

// Gửi encryptedSessionKeys lên server cùng với các thông tin tạo conversation
val requestBody = mapOf(
    "creatorId" to myUserId,
    "participantIds" to listOfUserIds,
    "type" to "Group", // hoặc "Private"
    "name" to "Tên nhóm",
    "encryptedSessionKeys" to encryptedSessionKeys
)
// Sử dụng retrofit hoặc http client để POST requestBody lên server



** Khi người dùng vào một boxchat
1. Gọi method JoinConversatioin để nhận publicKeys của các participants (để mã hóa khóa phiên), 
và nhận sessnkey (đã mã hóa, dùng để mã hóa và giải mã tin nhắn sau này) 
, gồm các bước sau:

a. Gọi method JoinConversation trên SignalR Hub
Giả sử bạn đã kết nối SignalR (HubConnection), bạn sẽ gọi:
hubConnection.send("JoinConversation", conversationId)

b. Lắng nghe sự kiện trả về từ server
Server sẽ trả về qua sự kiện ("ReceivePublicKeysAndSessionKey"):
hubConnection.on(
    "ReceivePublicKeysAndSessionKey",
    { publicKeys: Map<Int, String>, sessionKeyInfo: EncryptedSessionKeyInfo? ->
        // publicKeys: userId -> publicKey (base64)
        // sessionKeyInfo: EncryptedSessionKeyInfo(userId, encryptedSessionKey)
        // Lưu publicKeys và sessionKeyInfo vào local storage để dùng cho E2EE
    },
    object : TypeReference<Map<Int, String>>() {},
    EncryptedSessionKeyInfo::class.java
)

** Khi người dùng gửi tin nhắn:
1. Mã hóa tin nhắn
fun encryptMessage(message: String, sessionKey: SecretKey, iv: ByteArray): String {
    val cipher = Cipher.getInstance("AES/CBC/PKCS5Padding")
    cipher.init(Cipher.ENCRYPT_MODE, sessionKey, IvParameterSpec(iv))
    val encryptedBytes = cipher.doFinal(message.toByteArray(Charsets.UTF_8))
    return Base64.encodeToString(encryptedBytes, Base64.NO_WRAP)
}

2. Mã hóa khóa phiên
import java.security.PublicKey

fun encryptSessionKeyWithPublicKey(sessionKey: SecretKey, publicKey: PublicKey): String {
    val cipher = Cipher.getInstance("RSA/ECB/PKCS1Padding")
    cipher.init(Cipher.ENCRYPT_MODE, publicKey)
    val encrypted = cipher.doFinal(sessionKey.encoded)
    return Base64.encodeToString(encrypted, Base64.NO_WRAP)
}

** Khi người dùng nhận tin nhắn:
1. Giải mã khóa phiên
import java.security.PrivateKey
import javax.crypto.Cipher
import android.util.Base64

fun decryptSessionKeyWithPrivateKey(encryptedSessionKeyBase64: String, privateKey: PrivateKey): SecretKey {
    val cipher = Cipher.getInstance("RSA/ECB/PKCS1Padding")
    cipher.init(Cipher.DECRYPT_MODE, privateKey)
    val encryptedBytes = Base64.decode(encryptedSessionKeyBase64, Base64.DEFAULT)
    val sessionKeyBytes = cipher.doFinal(encryptedBytes)
    return javax.crypto.spec.SecretKeySpec(sessionKeyBytes, "AES")
}

2. Giải mã tin nhắn
import javax.crypto.Cipher
import javax.crypto.SecretKey
import javax.crypto.spec.IvParameterSpec

fun decryptMessage(encryptedMessageBase64: String, sessionKey: SecretKey, iv: ByteArray): String {
    val cipher = Cipher.getInstance("AES/CBC/PKCS5Padding")
    cipher.init(Cipher.DECRYPT_MODE, sessionKey, IvParameterSpec(iv))
    val encryptedBytes = Base64.decode(encryptedMessageBase64, Base64.DEFAULT)
    val decryptedBytes = cipher.doFinal(encryptedBytes)
    return String(decryptedBytes, Charsets.UTF_8)
}


3. Gửi tin nhắn và khóa phiên mã hóa
// Gửi lên server (ví dụ dùng retrofit hoặc signalR)
val sendMessageRequest = mapOf(
    "conversationId" to conversationId,
    "senderId" to myUserId,
    "content" to encryptMessage(message, sessionKey, iv),
    "messageType" to "Text"
    ...
)

** Xử lý thêm/loại bỏ thành viên (để sau)


****PlantUML
@startuml
participant Client
participant Server
participant DB

== Đăng ký tài khoản ==
Client -> Client: Tạo key pair (public/private)
Client -> Server: Đăng ký tài khoản kèm publicKey
Server -> DB: Lưu user info + publicKey

== Tạo cuộc trò chuyện ==
Client -> Server: Gọi API GetPublicKeys(userIds)
Server -> DB: Truy xuất publicKey của các user (kể cả userid người tạo)
Server -> Client: Trả về publicKeys

Client -> Client: Tạo sessionKey (AES)
Client -> Client: Mã hóa sessionKey bằng từng publicKey
Client -> Server: Gửi danh sách userIds + list(userId, encryptedSessionKey)
Server -> DB: Lưu conversation + EncryptedSessionKeys

== Tham gia cuộc trò chuyện ==
Client -> Server: SignalR send("JoinConversation", conversationId)
Server -> DB: Truy xuất EncryptedSessionKey của conversation
Server -> DB: Truy xuất publicKeys của các participants khác
Server -> Client: Trigger ReceivePublicKeysAndSessionKey(Map<int: userid, string: publickey>, sessionKeyInfo) // Khi gọi hàm này của Hub, client sẽ nhận 2 thứ: 1. map các publickey; 2. object chứa sessionkey được mã hóa bằng public key của người đó.

Client -> Client: Giải mã sessionKey lấy từ object sessionKeyInfo
Client -> Client: Lưu sessionKey và publicKeys vào local storage

== Gửi tin nhắn ==
Client -> Client: Mã hóa nội dung bằng sessionKey (AES/CBC)
Client -> Server: Gửi EncryptedMessage
Server -> DB: Lưu ChatMessage (message)
Server -> Client: Gửi EncryptedMessage tới các client trong group

== Nhận tin nhắn ==
Client -> Client: Giải mã nội dung tin nhắn bằng sessionKey từ local storage

== Thêm thành viên ==
Client -> Server: Yêu cầu thêm thành viên mới
Server -> DB: Cập nhật danh sách user
Server -> Client: Gửi publicKey của thành viên mới
Client -> Client: Mã hóa lại sessionKey cho thành viên mới
Client -> Server: Gửi encryptedSessionKey mới
Server -> DB: Cập nhật EncryptedSessionKeys

@enduml