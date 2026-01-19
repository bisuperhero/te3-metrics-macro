Poměrně často pracuji v Power BI s menšími sémantickými modely, které slouží pro rychlý ad-hoc reporting, PoC a podobně. Nad různými datovými zdroji pak pravidelně potřebuju vytvořit stále stejné typy DAX metrik. V mém případě se nejčastěji jedná o různé součty, průměry a počty záznamů. K tomu se mi pak často hodí i metriky pro sledování změn oproti dalším obdobím (např. předchozí měsíc, předchozí rok) a absolutní i procentuální změny. K tomu si občas přidávám ještě metriky s barvami pro podmíněné formátování a najednou se může jednat o docela velké množství nudného kódu, který je potřeba napsat. A tak jsem si vytvořil makro pro Tabular Editor 3, které mi tento proces výrazně urychluje. Tady ho máte.

## Vytvářené metriky

Script vychází z mých potřeb a nemusí tak plně odpovídat vašim požadavkům. Nicméně by měl být dostatečně přehledný, abyste si ho mohli upravit podle svých představ. Ve svém základu umožňuje pro vybraný sloupec vytvořit následující metriky:

* **SUM** nebo **SUMX** pro součty
* **COUNT** nebo **COUNTX** pro počty záznamů
* **DISTINCTCOUNT** pro počty unikátních hodnot

Pro každou z těchto metrik pak umožňuje vytvořit i následující doplňkové metriky:

* **PM (Previous Month)** - hodnota za předchozí měsíc
* **PQ (Previous Quarter)** - hodnota za předchozí čtvrtletí
* **PY (Previous Year)** - hodnota za předchozí rok
* **PP (Period over Period)** - absolutní změna oproti předchozímu období
* **RT (Running Total)** - průběžný (kumulativní součet)

U doplňkových metrik jsou navíc vytvářeny i metriky pro rozdíl mezi aktuální hodnotou a hodnotou z předchozího období (např. **MoM**, **YoY**) a procentuální změnu (např. **MoM %**, **YoY %**). Nakonec jsou ještě přidány metriky pro podmíněné formátování, které vrací barvu na základě hodnoty metriky (zelená pro růst, červená pro pokles) ve dvou variantách: barva písma nebo barva pozadí.

V neposlední řadě pak makro umožňuje vytvořit **metriky i pro neaktivní relace** mezi tabulky (pomocí funkce USERELATIONSHIP).

## Příprava sémantického modelu

Sémantický model není potřeba nikterak speciálně připravovat. Jedinou podmínkou je existence defaultní datumové tabulky, na kterou jsou napojeny ostatní tabulky, ve kterých budete chtít vytvářet metriky s časovou inteligencí. Makro v defaultní konfiguraci předpokládá, že se tabulka jmenuje `Date` a že obsahuje sloupec `Date[Date]`. Pokud máte tabulku pojmenovanou jinak, nebo používáte jiné názvy sloupců, je potřeba upravit konfiguraci makra (viz níže).

Společně s první metrikou se v modelu vytvářejí i pomocné metriky, ve kterých jsou definovány barvy pro podmíněné formátování. Ty se vytvářejí v předem definované tabulce `00 MEASURES`. Pokud chcete mít tyto metriky jinde, je potřeba opět upravit konfiguraci makra. Stejně tak v samotné konfiguraci můžete upravit i samotné barvy.

## Vytvoření makra v Tabular Editoru

1. Otevřete Tabular Editor 3 (v předchozí verzi vám bohužel fungovat nebude) a v menu **File** vyberte **New -> New C# Script**.
2. Zkopírujte níže uvedený kód do okna skriptu.
3. Upravte konfiguraci podle vašich potřeb (viz níže).
4. V menu **C# Script** vyberte **Save as Macro**. Zadejte název makra a zkontrolujte, že v části **Macro context** je zaškrtnuta hodnota **Column**. Ta zajistí, že makro bude dostupné při pravém kliknutí na sloupec v tabulce.


## Úprava konfigurace

Hned v úvodní části kódu makra najdete sekci `--- CONFIG ---`, kde můžete upravit následující hodnoty:
```cs
// --- CONFIG ---
string DEFAULT_DATE_TABLE = "Date"; // Name of the default date table
string DEFAULT_DATE_COLUMN = "Date"; // Name of the default date column
... 
```

* `DEFAULT_DATE_TABLE` - název tabulky, která obsahuje datumový sloupec pro časovou inteligenci (předvyplňuje se do vstupního pole ve formuláři)
* `DEFAULT_DATE_COLUMN` - název sloupce v datumové tabulce, který obsahuje datum (předvyplňuje se do vstupního pole ve formuláři)
* `DEFAULT_DATE_COLUMN_EXAMPLE` - příklad formátu pro zadání datumového sloupce (předvyplňuje se do vstupního pole ve formuláři)
* `DEFAULT_FORMAT_STRING` - výchozí formátovací řetězec pro vytvářené metriky je celé číslo s oddělovačem tisíců (můžete upravit podle svých preferencí)
* `MEASURES_TABLE_NAME` - název tabulky, ve které budou vytvořeny metriky s barvami pro podmíněné formátování (tabulka musí v modelu existovat)
* `COLORS_FOLDER` - název složky, ve které se metriky s barvami vytvoří
* `PREFIX_SUM`, `PREFIX_COUNT`, `PREFIX_DISTINCTCOUNT` - prefixy pro názvy vytvářených metrik (můžete upravit podle svých preferencí)
* `ALT_REL_FOLDER` - název složky, ve které se vytvoří metriky pro neaktivní relace
* `COLOR_GREEN`, `COLOR_GREEN_BG`, `COLOR_RED`, `COLOR_RED_BG` - barvy pro podmíněné formátování, suffix `_BG` říká, že se jedná o barvu pozadí (můžete upravit podle svých preferencí)

## DAX kód vytvářených metrik

Defaultní DAX kód je nastaven tak, aby vyhovoval mým potřebám. Pokud byste chtěli upravit způsob výpočtu metrik, můžete tak učinit v 

### Vzorový DAX kód pro SUM metriky sloupce `Sales[Amount]`:

```dax

---------------------------
-- Measure: [Ʃ Sales total]
---------------------------
MEASURE Sales[Ʃ Sales total] = SUM('Sales'[Amount])
    , Description = "This measure is the SUM of 'Sales'[Amount]."
    , DisplayFolder = "\Ʃ Sales total"
    , FormatString = "#,##0"

------------------------------
-- Measure: [RT Ʃ Sales total]
------------------------------
MEASURE Sales[RT Ʃ Sales total] = VAR __lastvisibledate = MAX('Date'[Date]) RETURN CALCULATE([Ʃ Sales total], 'Date'[Date] <= __lastvisibledate)
    , Description = "This measure is the running total of 'Sales'[Amount]."
    , DisplayFolder = "\Ʃ Sales total\RT"
    , FormatString = "#,##0"

------------------------------
-- Measure: [Ʃ Sales total PM]
------------------------------
MEASURE Sales[Ʃ Sales total PM] = CALCULATE([Ʃ Sales total], CALCULATETABLE(DATEADD('Date'[Date], -1, MONTH)))
    , Description = "This measure is the SUM of 'Sales'[Amount] for the previous month."
    , DisplayFolder = "\Ʃ Sales total\PM"
    , FormatString = "#,##0"

-------------------------------
-- Measure: [MoM Ʃ Sales total]
-------------------------------
MEASURE Sales[MoM Ʃ Sales total] = 
    VAR _currentp = [Ʃ Sales total]
    VAR _previous = [Ʃ Sales total PM]
    VAR _result = IF(NOT ISBLANK(_previous) && NOT ISBLANK(_currentp), _currentp - _previous)
    RETURN _result
    , Description = "This measure is the difference between current and previous month SUM of 'Sales'[Amount]."
    , DisplayFolder = "\Ʃ Sales total\PM"
    , FormatString = "#,##0"

---------------------------------
-- Measure: [MoM Ʃ Sales total %]
---------------------------------
MEASURE Sales[MoM Ʃ Sales total %] = DIVIDE([MoM Ʃ Sales total], [Ʃ Sales total PM])
    , Description = "This measure is the percentage change between current and previous month SUM of 'Sales'[Amount]."
    , DisplayFolder = "\Ʃ Sales total\PM"
    , FormatString = "#,##0.0%"

--------------------------------------
-- Measure: [_color MoM Ʃ Sales total]
--------------------------------------
MEASURE Sales[_color MoM Ʃ Sales total] = VAR __result = [MoM Ʃ Sales total] RETURN SWITCH(TRUE(), __result > 0, [_color_green], __result < 0, [_color_red], BLANK())
    , DisplayFolder = "\Ʃ Sales total\PM"
    , FormatString = "Text"

-----------------------------------------
-- Measure: [_color_bg MoM Ʃ Sales total]
-----------------------------------------
MEASURE Sales[_color_bg MoM Ʃ Sales total] = VAR __result = [MoM Ʃ Sales total] RETURN SWITCH(TRUE(), __result > 0, [_color_green_bg], __result < 0, [_color_red_bg], BLANK())
    , DisplayFolder = "\Ʃ Sales total\PM"
    , FormatString = "Text"

------------------------------
-- Measure: [Ʃ Sales total PQ]
------------------------------
MEASURE Sales[Ʃ Sales total PQ] = CALCULATE([Ʃ Sales total], CALCULATETABLE(DATEADD('Date'[Date], -1, QUARTER)))
    , Description = "This measure is the SUM of 'Sales'[Amount] for the previous quarter."
    , DisplayFolder = "\Ʃ Sales total\PQ"
    , FormatString = "#,##0"

-------------------------------
-- Measure: [QoQ Ʃ Sales total]
-------------------------------
MEASURE Sales[QoQ Ʃ Sales total] = 
    VAR _currentp = [Ʃ Sales total]
    VAR _previous = [Ʃ Sales total PQ]
    VAR _result = IF(NOT ISBLANK(_previous) && NOT ISBLANK(_currentp), _currentp - _previous)
    RETURN _result
    , Description = "This measure is the difference between current and previous quarter SUM of 'Sales'[Amount]."
    , DisplayFolder = "\Ʃ Sales total\PQ"
    , FormatString = "#,##0"

---------------------------------
-- Measure: [QoQ Ʃ Sales total %]
---------------------------------
MEASURE Sales[QoQ Ʃ Sales total %] = DIVIDE([QoQ Ʃ Sales total], [Ʃ Sales total PQ])
    , Description = "This measure is the percentage change between current and previous quarter SUM of 'Sales'[Amount]."
    , DisplayFolder = "\Ʃ Sales total\PQ"
    , FormatString = "#,##0.0%"

--------------------------------------
-- Measure: [_color QoQ Ʃ Sales total]
--------------------------------------
MEASURE Sales[_color QoQ Ʃ Sales total] = VAR __result = [QoQ Ʃ Sales total] RETURN SWITCH(TRUE(), __result > 0, [_color_green], __result < 0, [_color_red], BLANK())
    , DisplayFolder = "\Ʃ Sales total\PQ"
    , FormatString = "Text"

-----------------------------------------
-- Measure: [_color_bg QoQ Ʃ Sales total]
-----------------------------------------
MEASURE Sales[_color_bg QoQ Ʃ Sales total] = VAR __result = [QoQ Ʃ Sales total] RETURN SWITCH(TRUE(), __result > 0, [_color_green_bg], __result < 0, [_color_red_bg], BLANK())
    , DisplayFolder = "\Ʃ Sales total\PQ"
    , FormatString = "Text"

------------------------------
-- Measure: [Ʃ Sales total PY]
------------------------------
MEASURE Sales[Ʃ Sales total PY] = CALCULATE([Ʃ Sales total], CALCULATETABLE(DATEADD('Date'[Date], -1, YEAR)))
    , Description = "This measure is the SUM of 'Sales'[Amount] for the previous year."
    , DisplayFolder = "\Ʃ Sales total\PY"
    , FormatString = "#,##0"

-------------------------------
-- Measure: [YoY Ʃ Sales total]
-------------------------------
MEASURE Sales[YoY Ʃ Sales total] = 
    VAR _currentp = [Ʃ Sales total]
    VAR _previous = [Ʃ Sales total PY]
    VAR _result = IF(NOT ISBLANK(_previous) && NOT ISBLANK(_currentp), _currentp - _previous)
    RETURN _result
    , Description = "This measure is the difference between current and previous year SUM of 'Sales'[Amount]."
    , DisplayFolder = "\Ʃ Sales total\PY"
    , FormatString = "#,##0"

---------------------------------
-- Measure: [YoY Ʃ Sales total %]
---------------------------------
MEASURE Sales[YoY Ʃ Sales total %] = DIVIDE([YoY Ʃ Sales total], [Ʃ Sales total PY])
    , Description = "This measure is the percentage change between current and previous year SUM of 'Sales'[Amount]."
    , DisplayFolder = "\Ʃ Sales total\PY"
    , FormatString = "#,##0.0%"

--------------------------------------
-- Measure: [_color YoY Ʃ Sales total]
--------------------------------------
MEASURE Sales[_color YoY Ʃ Sales total] = VAR __result = [YoY Ʃ Sales total] RETURN SWITCH(TRUE(), __result > 0, [_color_green], __result < 0, [_color_red], BLANK())
    , DisplayFolder = "\Ʃ Sales total\PY"
    , FormatString = "Text"

-----------------------------------------
-- Measure: [_color_bg YoY Ʃ Sales total]
-----------------------------------------
MEASURE Sales[_color_bg YoY Ʃ Sales total] = VAR __result = [YoY Ʃ Sales total] RETURN SWITCH(TRUE(), __result > 0, [_color_green_bg], __result < 0, [_color_red_bg], BLANK())
    , DisplayFolder = "\Ʃ Sales total\PY"
    , FormatString = "Text"

------------------------------
-- Measure: [Ʃ Sales total PP]
------------------------------
MEASURE Sales[Ʃ Sales total PP] = VAR __min = MIN('Date'[Date]) VAR __max = MAX('Date'[Date]) VAR __days = DATEDIFF(__max, __min, DAY) -1  RETURN CALCULATE ([Ʃ Sales total], DATEADD('Date'[Date], __days, DAY))
    , Description = "This measure is the SUM of 'Sales'[Amount] for the previous period."
    , DisplayFolder = "\Ʃ Sales total\PP"
    , FormatString = "#,##0"

-------------------------------
-- Measure: [PoP Ʃ Sales total]
-------------------------------
MEASURE Sales[PoP Ʃ Sales total] = 
    VAR _currentp = [Ʃ Sales total]
    VAR _previous = [Ʃ Sales total PP]
    VAR _result = IF(NOT ISBLANK(_previous) && NOT ISBLANK(_currentp), _currentp - _previous)
    RETURN _result
    , Description = "This measure is the difference between current and previous period SUM of 'Sales'[Amount]."
    , DisplayFolder = "\Ʃ Sales total\PP"
    , FormatString = "#,##0"

---------------------------------
-- Measure: [PoP Ʃ Sales total %]
---------------------------------
MEASURE Sales[PoP Ʃ Sales total %] = DIVIDE([PoP Ʃ Sales total], [Ʃ Sales total PP])
    , Description = "This measure is the percentage change between current and previous period SUM of 'Sales'[Amount]."
    , DisplayFolder = "\Ʃ Sales total\PP"
    , FormatString = "#,##0.0%"

--------------------------------------
-- Measure: [_color PoP Ʃ Sales total]
--------------------------------------
MEASURE Sales[_color PoP Ʃ Sales total] = VAR __result = [PoP Ʃ Sales total] RETURN SWITCH(TRUE(), __result > 0, [_color_green], __result < 0, [_color_red], BLANK())
    , DisplayFolder = "\Ʃ Sales total\PP"
    , FormatString = "Text"

-----------------------------------------
-- Measure: [_color_bg PoP Ʃ Sales total]
-----------------------------------------
MEASURE Sales[_color_bg PoP Ʃ Sales total] = VAR __result = [PoP Ʃ Sales total] RETURN SWITCH(TRUE(), __result > 0, [_color_green_bg], __result < 0, [_color_red_bg], BLANK())
    , DisplayFolder = "\Ʃ Sales total\PP"
    , FormatString = "Text"

```

## Celý kód

Celý kód makra naleznete na mém GitHubu, případné chyby nebo návrhy na vylepšení mi můžete poslat prostřednictvím issues nebo pull requestů.

<a href="https://github.com/bisuperhero/te3-metrics-macro" class="blog-button" target="_blank" rel="noopener">Kód makra</a>
