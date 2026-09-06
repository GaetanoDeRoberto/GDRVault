# GDRVault.Bridge — v1

Questo progetto è il ponte tra l'estensione del browser e l'app desktop GDRVault.

## Cosa implementa

- protocollo Native Messaging su stdin/stdout;
- messaggio `ping`;
- richiesta `get_credentials`;
- comunicazione locale con GDRVault tramite Named Pipe;
- limite di 1 MB per messaggio;
- nessuna password salvata dal Bridge.

## Protocollo estensione → Bridge

```json
{
  "action": "get_credentials",
  "domain": "example.com"
}
```

## Protocollo Bridge → GDRVault

Named Pipe:

`GDRVault.Autofill`

```json
{
  "action": "get_credentials",
  "domain": "example.com"
}
```

GDRVault dovrà rispondere con:

```json
{
  "success": true,
  "credentials": [
    {
      "id": "00000000-0000-0000-0000-000000000000",
      "title": "Example",
      "username": "utente@example.com",
      "password": "..."
      "url": "https://example.com"
    }
  ]
}
```

## Nota importante

Il Bridge NON apre direttamente il database e NON conosce la Master Password.

La prossima integrazione necessaria è lato app GDRVault: un server Named Pipe che risponda alle richieste del Bridge utilizzando il Vault già aperto e sbloccato.

Dopo questo verrà creata l'estensione Chrome/Edge e il relativo manifest Native Messaging.
