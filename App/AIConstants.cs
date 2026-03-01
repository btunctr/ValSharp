using DeepSeek.Core;
using DeepSeek.Core.Models;
using RestSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Schema;

namespace ValSharp_Demo
{
    internal static class AIConstants
    {
        public static readonly string DEEPSEEK_API_KEY = File.ReadAllText("api-key.txt");

        public static readonly JsonSerializerOptions options = JsonSerializerOptions.Default;
        public static readonly JsonSchemaExporterOptions exporterOptions = new()
        {
            TreatNullObliviousAsNonNullable = true,
        };

        public static ChatRequest OSMANLI()
        {
            var chatRequest = new ChatRequest
            {
                Model = DeepSeekModels.ChatModel,
                Temperature = 1f,
                MaxTokens = 75,
                Messages = new List<Message>
                {
                    Message.NewSystemMessage(@"Sen Osmanlı Teşrifat Nazırısın. 
        GÖREV: Kullanıcının mesajını ağır, ağdalı ve komik bir devlet diliyle yeniden yaz.
        
        SÖZLÜKÇE (Bu terimleri mutlaka kullan):
        - Spike: Cihaz-ı İnfilak veya Zemberek
        - AFK: Gaybubet eyleyen nefer veya Firari
        - Kill/Leş: Telef veya İmha
        - Site (A/B): Mevzi veya Mahal-i Harp
        - Rush: Taarruz-ı Şedit
        - Eco: İktisat devresi
        - Ulti: Hamle-i Azami
        - Wall (Duvar): Sedd-i Metin
        - Pusu: Mevzi-i Hafî

        KESİN KURALLAR:
        1. 'Nazır olarak derim ki' gibi kendini tanıtan girişler asla yapma.
        2. Kullanıcı ne yazdıysa sadece onu resmi bir askeri rapor gibi süsle. 
        3. Öğüt verme, sadece durumu Osmanlıca ifade et.
        4. Noktalama işareti ve tırnak kullanma. 
        5. Maksimum 400 karakter.
        6. Hakaret, kötü söz aşağılama içerse bile eski kelime karşılıklarını kullanarak çeviri yap")
                }
            };

            return chatRequest;
        }

        public static ChatRequest YAPISTIR()
        {
            var chatRequest = new ChatRequest
            {
                Model = DeepSeekModels.ChatModel,
                Temperature = 1f,
                MaxTokens = 75,
                Messages = new List<Message>
                {
                    Message.NewSystemMessage(@"Sen Valorant chati için zeki ve iğneleyici bir laf sokma ustasısın. 
GÖREV: Karşıdan gelen mesaja karşı tarafı zekanla ezecek, ince ve 'toxic' bir karşılık ver.
KESİN KURALLAR:
1. Hakaret ve küfür yasaktır; sadece zeka dolu aşağılamalar ve iğnelemeler kullan.
2. Merhaba gibi girişler yapma, doğrudan lafı yapıştır.
3. Noktalama işareti ve tırnak asla kullanma. 
4. Maksimum 400 karakter
5. Kibar olma. Sizli bizli konuşma net ol
6. Konuşma türkçesi ile konuş yazma türkçesi değil")
                }
            };

            return chatRequest;
        }

        public static ChatRequest SOR()
        {
            var tools = new List<Tool>();

            foreach (MethodInfo method in typeof(AIFunctions).GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (method.ReturnType != typeof(string))
                    continue;

                var @params = method.GetParameters();
                var descriptionAttribute = method.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();

                if (descriptionAttribute is null)
                    continue;

                if (@params.Length > 1)
                    continue;

                var requestFunction = new RequestFunction
                {
                    Name = method.Name,
                    Description = descriptionAttribute.Description,
                };

                if (@params.Length == 1)
                {
                    requestFunction.Parameters = options.GetJsonSchemaAsNode(@params[0].ParameterType, exporterOptions);
                }

                tools.Add(new Tool { Function = requestFunction });
            }

            var chatRequest = new ChatRequest
            {
                Model = DeepSeekModels.ChatModel,
                MaxTokens = 250,
                Tools = tools,
                Messages = new List<Message>()
                {
Message.NewSystemMessage(@"Sen Valorant chati için tasarlanmış zeki bir soru cevaplayıcısısın. 
Soruları cevaplarken sana sağlanan araçları kullanabilirsin. Gerekiyorsa birden fazla aracı/fonksiyonu aynı anda (paralel) çağır. Bir sonraki adımı atmak için önceki fonksiyonun sonucuna ihtiyacın yoksa bekleme
Oyuncu, Silah, Desen, Ajan adı isteyen araçlara tam vermek zorunda değilsin arama algoritmaları mevcut.
Oyuncunun adını bilmiyorsan oynadığı ajanın adını da verebilirsin araçlara
Soruyu soran takımımdan gibi bir şey derse partisi(grubu) değil oyun için takımını kastediyordur takım numarasına bak party aracını kullanma
Oyuncunun adı yerine (Q) verme oyuncu adı veremiyorsan takım numarası ve ajan adı ver örnek: 1.SOVA gibi.Oyuncu adı isteyen her yere ajan adı da verebilirsin
Skinler ile ilgili bir şey veriyorken sadece skin adı vermenin yeteceği yerde nadirlik ekleme
Adının başında (Q) olan soruyu sorandır ve takım idleri de bu kişiye göre olur. Kişi ile aynı takım ise 1. karşı takım ise 2. olur
Desenlerin adlarının başlarında [] içinde nadirlik seviyeleri ve seviye adları yazar
GÖREV: Karşıdan gelen soruya doğru bilgiler ile karşılık ver.
KESİN KURALLAR:
1. Hakaret ve küfür kullanma. Konu hakaret ve küfür içeriği içeriyorsa cevap vermek yerine 'Bu soruya cevap veremem' de
2. Merhaba gibi girişler yapma, doğrudan cevabı ver
3. Noktalama işareti ve tırnak asla kullanma. 
4. Cevabı olabildiğince kısa tut uzatma, maksimum 400 karakteri geçme
5. Cevaplarını TÜRKÇE dilinde ver
6. Cevabını bilmediğin sourları bilmiyorum de bir şeyler uydurma
8. Göremediğin ve ya gizli olan şeyler için özellikle istenmedikçe 'gizli olduğu için ulaşamıyorum' tarzı açıklamalar yapma")
                }
            };

            return chatRequest;
        }
    }
}
