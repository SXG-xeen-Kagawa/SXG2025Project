# 戦車のカスタマイズ方法

自身の戦車を開き、Prefabの編集モード内の「戦車Prefab編集モードパネル」で、自身の戦車に砲塔や装甲を着けてカスタマイズできます。

<img width="1512" height="468" alt="image" src="https://github.com/user-attachments/assets/426fe414-2a8a-4875-b3ab-ebab541b48fe" />

砲塔や装甲の装着には「コスト」がかかり、それらの総コストが１台の戦車を出撃させるのに必要なコスト「**出撃コスト**」となります。  
各プレイヤーは**１試合 1000コスト分 までの戦車を出撃できます。**  
つまり、出撃コストが200の場合、１試合の出撃可能回数は５回となります。  
また、何もつけていない初期状態の場合の出撃コストは100です。

装甲などをたくさんつけると、その分戦車が重くなります。  
戦車が重くなるほど移動速度が遅くなってしまいますが、砲弾を発射したときの反動が小さくなるメリットがあります。 

---

## 砲塔を追加する

「戦車Prefab編集モードパネル」で「砲塔追加」ボタンを押すと、砲塔が追加されます。  
砲塔をつけると、砲弾を発射して他の戦車を攻撃できます。  

砲塔１基のコストは「50」です。  
砲塔は0基でも、複数台つけても構いません。    

<img width="1512" height="468" alt="image" src="https://github.com/user-attachments/assets/8ea633f9-a7e6-42ad-93a1-fb28ed16042f" />

&nbsp;

追加した砲台は、スクリプトのインスペクタの「Turrets」のリストにも追加されます。  
スクリプトで砲台番号を指定する際は、Turretsリストのインデックスを指定してください。

※砲塔のスケールは変更しないでください

---

## 装甲をつける

戦車に好きなモデルの装甲をつけられます。  
コストはモデルの体積によって変動しますが、最小コストは「1」です。

**※あまりにも大きなサイズのモデルの使用はご遠慮ください**  
**※SkinnedMeshRendererは装甲として計上されませんので、MeshRendererを使用してください**  
**※装甲はPrefabの直下（子）に配置してください**

&nbsp;

Prefab内でCubeを作成し、Transform（座標・回転・スケール）を調整するだけでも、装甲の出来上がりです。  

<img width="1917" height="467" alt="image" src="https://github.com/user-attachments/assets/a736600d-bad0-4183-8f7d-13e058547d75" />

&nbsp;

装甲にテクスチャを貼り付ければ、戦車が更にカッコよく見えます。  

1. 好きなテクスチャをインポートする
2. 新規マテリアルを作成する
3. マテリアルのインスペクタの「Base Map」に、インポートしたテクスチャをアタッチ
4. Prefab内のCubeオブジェクトに、作成したマテリアルをアタッチ

<img width="1919" height="485" alt="image" src="https://github.com/user-attachments/assets/5f988732-a637-48d0-872e-fd744cc861a8" />

&nbsp;

`Assets/BattleTanks/Prefabs/SampleTank/PartsModel/`  
また、上記のフォルダ内に、弊社がいくつかモデルを用意していますので、ご自由にご利用ください。  
スケールを変更しても構いません。

<img width="237" height="234" alt="image" src="https://github.com/user-attachments/assets/d5fdbae6-de24-4304-99c0-e7390f502769" />

<img width="1512" height="473" alt="image" src="https://github.com/user-attachments/assets/082d5d4c-ec12-4729-80d9-380978c4ebdd" />

---

## 回転部位を追加する

「戦車Prefab編集モードパネル」で「回転部位追加」ボタンを押すと、回転部位が追加されます。  
回転部位をつけて回転させることで、子にした装甲も一緒に回転させることができます。

回転部位１基のコストは「2」です。  
回転部位は0基でも、複数つけても構いません。

<img width="1512" height="473" alt="image" src="https://github.com/user-attachments/assets/e9049fbb-bca2-4920-9e7d-d741c055df1e" />

<img width="310" height="153" alt="image" src="https://github.com/user-attachments/assets/55795c28-3a4d-4109-9427-03345913b5ac" />

&nbsp;

追加した回転部位は、スクリプトのインスペクタの「RotJoints」のリストにも追加されます。  
スクリプトで回転部位の番号を指定する際は、RotJointsリストのインデックスを指定してください。

---

## 攻撃が当たる部分のコリジョン表示

「戦車Prefab編集モードパネル」で「コリジョン表示」ボタンを押すと、砲弾による攻撃が当たる部分のコリジョンの表示/非表示を切り替えられます。  
半透明の緑色で表示されます。  

<img width="630" height="360" alt="image" src="https://github.com/user-attachments/assets/67102d96-19e7-4ce9-9eba-1f4936b07ff6" />

&nbsp;

ボタンを押してもコリジョンが表示されない場合は、エディタ上のこのボタンが有効になっているか確認してください。

<img width="290" height="92" alt="image" src="https://github.com/user-attachments/assets/19359e7b-e7dd-43c2-8187-ff9a7fd9885d" />


