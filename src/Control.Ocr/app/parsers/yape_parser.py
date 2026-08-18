from __future__ import annotations

import re

from app.models.ocr_result import OcrDocument, OcrLine, ReceiptAnalysis
from app.parsers.generic_receipt_parser import GenericReceiptParser, MONEY_PATTERN
from app.parsers.helpers.date_parser import has_spanish_date, parse_spanish_datetime

OPERATION_PATTERN = re.compile(r"\b\d{6,}\b")


class YapeParser(GenericReceiptParser):
    def matches(self, document: OcrDocument) -> bool:
        return any("yape" in line.text.casefold() for line in document.lines)

    def parse(self, document: OcrDocument) -> ReceiptAnalysis:
        amount = self._best_yape_amount(document)
        destination = self._value_after_label(document.lines, "destino")
        operation_number = self._operation_number(document.lines)
        recipient = self._recipient_near_date(document.lines)
        transaction_date = self._transaction_date(document.lines)

        if amount is None:
            ambiguous = self._principal_ambiguous_line(document)
            return ReceiptAnalysis(
                document_type="YAPE",
                amount_raw=ambiguous.text if ambiguous else None,
                date=transaction_date,
                recipient=recipient,
                destination=destination,
                operation_number=operation_number,
                confidence=round(ambiguous.confidence or 0.0, 2) if ambiguous else 0.0,
                requires_review=True,
                ocr_lines=document.lines,
            )

        line, value = amount
        confidence = line.confidence or 0.0
        return ReceiptAnalysis(
            document_type="YAPE",
            amount=value,
            amount_raw=line.text,
            currency="PEN",
            date=transaction_date,
            recipient=recipient,
            destination=destination,
            operation_number=operation_number,
            confidence=round(confidence, 2),
            requires_review=confidence < 0.80,
            ocr_lines=document.lines,
        )

    def _best_yape_amount(self, document: OcrDocument) -> tuple[OcrLine, float] | None:
        candidates: list[tuple[float, OcrLine, float]] = []
        for line in document.lines:
            match = MONEY_PATTERN.search(line.text)
            if match is None or not self._is_near_top(line, document):
                continue
            # This parser handles Yape; a visible S/ or PEN is required evidence.
            if not match.group(0).upper().startswith(("S/", "PEN")):
                continue
            candidates.append((self._visual_score(line), line, float(match.group(1).replace(",", "."))))

        for currency_line, number_line in zip(document.lines, document.lines[1:]):
            if currency_line.text.strip().upper() not in {"S/", "PEN"}:
                continue
            if not re.fullmatch(r"\d+(?:[.,]\d{2})?", number_line.text.strip()):
                continue
            if not self._is_near_top(number_line, document) or not self._same_visual_row(currency_line, number_line):
                continue
            candidates.append((self._visual_score(number_line), number_line, float(number_line.text.replace(",", "."))))

        if not candidates:
            return None
        _, line, value = max(candidates, key=lambda candidate: candidate[0])
        return line, value

    @staticmethod
    def _is_near_top(line: OcrLine, document: OcrDocument) -> bool:
        return line.box is None or document.image_height is None or line.box.center_y <= document.image_height * 0.45

    @staticmethod
    def _same_visual_row(left: OcrLine, right: OcrLine) -> bool:
        if left.box is None or right.box is None:
            return False
        vertical_distance = abs(left.box.center_y - right.box.center_y)
        return vertical_distance <= max(left.box.height, right.box.height) * 0.6

    @staticmethod
    def _visual_score(line: OcrLine) -> float:
        # Larger detected text is a useful tie-breaker, never a substitute for a valid amount.
        return (line.confidence or 0.0) + min((line.box.height if line.box else 0) / 10_000, 0.05)

    @staticmethod
    def _principal_ambiguous_line(document: OcrDocument) -> OcrLine | None:
        candidates = [
            line for line in document.lines
            if re.fullmatch(r"\s*[\dS/.,]+\s*", line.text)
            and re.search(r"\d", line.text)
            and re.search(r"(?:S/|[.,/])", line.text, re.IGNORECASE)
            and YapeParser._is_near_top(line, document)
        ]
        if not candidates:
            return None
        return max(candidates, key=lambda line: ((line.box.height if line.box else 0), line.confidence or 0.0))

    @staticmethod
    def _value_after_label(lines: list[OcrLine], label: str) -> str | None:
        for index, line in enumerate(lines[:-1]):
            if line.text.strip().casefold() == label:
                return lines[index + 1].text.strip() or None
        return None

    @staticmethod
    def _operation_number(lines: list[OcrLine]) -> str | None:
        for index, line in enumerate(lines[:-1]):
            if "operación" in line.text.casefold() or "operacion" in line.text.casefold():
                match = OPERATION_PATTERN.search(lines[index + 1].text)
                return match.group(0) if match else None
        return None

    @staticmethod
    def _recipient_near_date(lines: list[OcrLine]) -> str | None:
        for index, line in enumerate(lines):
            if has_spanish_date(line.text) and index > 0:
                value = lines[index - 1].text.strip()
                return value if value and "yape" not in value.casefold() else None
        return None

    @staticmethod
    def _transaction_date(lines: list[OcrLine]) -> str | None:
        for line in lines:
            parsed = parse_spanish_datetime(line.text)
            if parsed is not None:
                return parsed.isoformat()
        return None
