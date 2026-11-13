import {createRoot} from 'react-dom/client';
import {GameLandscapeReview} from '../src/shared/buildManagement/GameLandscapeReview.tsx';
import '../src/styles.css';
createRoot(document.getElementById('root')!).render(<main className="content"><h1>Admin 빌드 검수 기준</h1><p>표시 전용 격리 검수 · 서버/게임 연결 없음</p><GameLandscapeReview/></main>);
