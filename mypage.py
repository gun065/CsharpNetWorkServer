#마이페이지 상세 버튼 화면
#mypage.py
import sys
from PySide6.QtCore import QCoreApplication, QMetaObject, QSize, Qt
from PySide6.QtGui import QFont
from PySide6.QtWidgets import QApplication, QPushButton, QSizePolicy, QSpacerItem, QVBoxLayout, QHBoxLayout, QWidget

class Ui_MyPage(object):
    def setupUi(self, MyPage):
        if not MyPage.objectName():
            MyPage.setObjectName(u"MyPage")
        MyPage.resize(430, 900)
        MyPage.setMinimumSize(QSize(430, 900))
        MyPage.setMaximumSize(QSize(430, 900))
        
        self.verticalLayout = QVBoxLayout(MyPage)
        self.verticalLayout.setObjectName(u"verticalLayout")
        self.verticalLayout.setSpacing(20)
        self.verticalLayout.setContentsMargins(30, 40, 30, -1)
        
        self.horizontalLayout_top = QHBoxLayout()
        self.horizontalLayout_top.setObjectName(u"horizontalLayout_top")
        
        self.btn_back = QPushButton(MyPage)
        self.btn_back.setObjectName(u"btn_back")
        self.btn_back.setMinimumSize(QSize(40, 40))
        self.btn_back.setMaximumSize(QSize(40, 40))
        self.btn_back.setText("<")
        self.horizontalLayout_top.addWidget(self.btn_back)
        
        self.horizontalSpacer = QSpacerItem(40, 20, QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Minimum)
        self.horizontalLayout_top.addItem(self.horizontalSpacer)
        
        self.btn_close = QPushButton(MyPage)
        self.btn_close.setObjectName(u"btn_close")
        self.btn_close.setMinimumSize(QSize(40, 40))
        self.btn_close.setMaximumSize(QSize(40, 40))
        self.btn_close.setText("X")
        self.horizontalLayout_top.addWidget(self.btn_close)
        
        self.verticalLayout.addLayout(self.horizontalLayout_top)
        
        self.btn_user_id = QPushButton(MyPage)
        self.btn_user_id.setObjectName(u"btn_user_id")
        self.btn_user_id.setMinimumSize(QSize(0, 50))
        font = QFont()
        font.setPointSize(9)
        font.setBold(False)
        self.btn_user_id.setFont(font)
        self.btn_user_id.setText("회원 아이디")
        self.verticalLayout.addWidget(self.btn_user_id)
        
        self.btn_update = QPushButton(MyPage)
        self.btn_update.setObjectName(u"btn_update")
        self.btn_update.setMinimumSize(QSize(0, 50))
        self.btn_update.setText("회원정보 수정")
        self.verticalLayout.addWidget(self.btn_update)
        
        self.btn_recent = QPushButton(MyPage)
        self.btn_recent.setObjectName(u"btn_recent")
        self.btn_recent.setMinimumSize(QSize(0, 50))
        self.btn_recent.setText("최근 본 웹툰")
        self.verticalLayout.addWidget(self.btn_recent)
        
        self.btn_comments = QPushButton(MyPage)
        self.btn_comments.setObjectName(u"btn_comments")
        self.btn_comments.setMinimumSize(QSize(0, 50))
        self.btn_comments.setText("내가 쓴 댓글 관리")
        self.verticalLayout.addWidget(self.btn_comments)
        
        self.btn_logout = QPushButton(MyPage)
        self.btn_logout.setObjectName(u"btn_logout")
        self.btn_logout.setMinimumSize(QSize(0, 50))
        self.btn_logout.setText("로그아웃")
        self.verticalLayout.addWidget(self.btn_logout)
        
        self.verticalSpacer_bottom = QSpacerItem(20, 40, QSizePolicy.Policy.Minimum, QSizePolicy.Policy.Expanding)
        self.verticalLayout.addItem(self.verticalSpacer_bottom)

        self.retranslateUi(MyPage)
        QMetaObject.connectSlotsByName(MyPage)

    def retranslateUi(self, MyPage):
        MyPage.setWindowTitle(QCoreApplication.translate("MyPage", u"마이페이지", None))

# ==========================================
# [팀원 확인용] .py 파일 단독 실행용 코드
# ==========================================
if __name__ == "__main__":
    app = QApplication(sys.argv)
    window = QWidget()
    ui = Ui_MyPage()
    ui.setupUi(window)
    window.show()
    sys.exit(app.exec())