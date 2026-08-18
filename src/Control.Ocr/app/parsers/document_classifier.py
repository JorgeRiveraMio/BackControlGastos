from __future__ import annotations

from enum import Enum

from app.models.ocr_result import OcrDocument


class DocumentType(str, Enum):
    YAPE = "YAPE"
    PLIN = "PLIN"
    GENERIC = "GENERIC"


class DocumentClassifier:
    """Classifies receipt origin using structural labels, not destination names."""

    _YAPE_SIGNALS = {
        "yapeaste": 4,
        "datos de la transacción": 3,
        "datos de la transaccion": 3,
        "nro. de celular": 1,
        "nro de celular": 1,
        "nro. de operación": 1,
        "nro de operación": 1,
        "nro. de operacion": 1,
        "nro de operacion": 1,
    }
    _PLIN_SIGNALS = {
        "pago exitoso": 3,
        "enviado a:": 2,
        "comisión:": 1,
        "comision:": 1,
        "fecha y hora:": 1,
        "código de operación:": 2,
        "codigo de operacion:": 2,
        "interbank": 2,
    }
    _MINIMUM_SCORE = 4

    def detect(self, document: OcrDocument) -> DocumentType:
        document_type, _ = self.detect_with_confidence(document)
        return document_type

    def detect_with_confidence(self, document: OcrDocument) -> tuple[DocumentType, float]:
        text_lines = [line.text.casefold() for line in document.lines]
        yape_score = self._score(text_lines, self._YAPE_SIGNALS)
        plin_score = self._score(text_lines, self._PLIN_SIGNALS)

        # Brand words count only after structural evidence is already present.
        if yape_score >= self._MINIMUM_SCORE and any(line.strip() == "yape" for line in text_lines):
            yape_score += 1
        if plin_score >= self._MINIMUM_SCORE and any(line.strip() == "plin" for line in text_lines):
            plin_score += 1

        if yape_score >= self._MINIMUM_SCORE and yape_score > plin_score:
            return DocumentType.YAPE, min(yape_score / 8, 0.99)
        if plin_score >= self._MINIMUM_SCORE and plin_score > yape_score:
            return DocumentType.PLIN, min(plin_score / 8, 0.99)
        return DocumentType.GENERIC, 0.0

    @staticmethod
    def _score(lines: list[str], signals: dict[str, int]) -> int:
        return sum(weight for signal, weight in signals.items() if any(signal in line for line in lines))
